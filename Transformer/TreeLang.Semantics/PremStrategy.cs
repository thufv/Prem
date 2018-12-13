using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Utils;
using Microsoft.ProgramSynthesis.VersionSpace;

using Prem.Util;
using PremLogger = Prem.Util.Logger;

namespace Prem.Transformer.TreeLang
{
    public class PremStrategy : SynthesisStrategy<ExampleSpec>
    {
        private static PremLogger Log = PremLogger.Instance;

        private static void Show<T>(T item)
        {
            Log.Fine("Candidate: {0}", item);
        }

        private static void ShowMany<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidates: {0}", String.Join(", ", list.Select(x => x.ToString())));
        }

        private static void ShowList<T>(IEnumerable<T> list)
        {
            Log.Fine("Candidate: [{0}]", String.Join(", ", list.Select(x => x.ToString())));
        }

        private Symbol _inputSymbol;

        private Grammar _grammar;

        private ProgramNode Input;

        private TInput GetInput(State input) => (TInput)input[_inputSymbol];

        private SyntaxNode GetSource(State input) => GetInput(input).errNode;

        private Result GetResult(State input) => GetSource(input).context.result;

        private int Find(State input, string s) =>
            ((TInput)input[_inputSymbol]).Find(s).ValueOr(-1);

        public PremStrategy(Grammar grammar) : base()
        {
            this._grammar = grammar;
            this._inputSymbol = grammar.InputSymbol;
            this.Input = new VariableNode(grammar.InputSymbol);
        }

        /* A bunch of handy functions for constructing program nodes/rules. */
        private Symbol Symbol(string name) => _grammar.Symbol(name);

        private NonterminalRule Op(string name) => (NonterminalRule)_grammar.Rule(name);

        private ProgramNode ChildIndex(int index) => new LiteralNode(Symbol("child"), index);
        
        private ProgramNode K(int k) => new LiteralNode(Symbol("k"), k);

        private ProgramNode S(string s) => new LiteralNode(Symbol("s"), s);

        private ProgramNode Label(Label label) => new LiteralNode(Symbol("label"), label);

        private ProgramNode Cursor(Cursor cursor) => new LiteralNode(Symbol("cursor"), cursor);

        private ProgramNode Just() => new NonterminalNode(Op("Just"), Input);

        private ProgramNode Move(Cursor cursor) => new NonterminalNode(Op("Move"), Input, Cursor(cursor));

        private ProgramNode Const(string s) => new NonterminalNode(Op("Const"), S(s));

        private ProgramNode Var(int i) => new NonterminalNode(Op("Var"), K(i));

        public override Optional<ProgramSet> Learn(SynthesisEngine engine, LearningTask<ExampleSpec> task,
            CancellationToken cancel)
        {
            var spec = task.Spec;
#if DEBUG
            Log.Fine("program |- {0}", spec);
#endif
            return LearnProgram(PremSpec<TInput, SyntaxNode>.From(spec.Examples, GetInput, o => (SyntaxNode)o));
        }

        private Optional<ProgramSet> LearnProgram(PremSpec<TInput, SyntaxNode> spec)
        {
            var kinds = spec.MapInputs(i => i.errNode.context.result.kind);
            if (!kinds.Same())
            {
                return Optional<ProgramSet>.Nothing;
            }

            switch (spec.First().Key.errNode.context.result.kind)
            {
                case ResultKind.INSERT: // Use `Ins`.
                {
                    // Synthesize param `child`.
                    var ks = spec.MapInputs(i => i.errNode.context.result).Select(r => (Insert)r).Select(r => r.k);
                    if (!ks.Same())
                    {
                        return Optional<ProgramSet>.Nothing;
                    }
                    var k = ks.First();
                    var kSpace = ProgramSet.List(Symbol("child"), ChildIndex(k));

                    // Synthesize param `ref`.
                    var refSpec = spec.MapOutputs((i, o) => ((Insert)i.errNode.context.result).oldNodeParent);
                    var refSpace = LearnRef(refSpec, "Ins");
                    if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    // Synthesize param `tree`.
                    var treeSpec = spec.MapOutputs((i, o) => ((Insert)i.errNode.context.result).newNode);
                    var treeSpace = LearnTree(treeSpec, "Ins");
                    if (!treeSpace.HasValue || treeSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    // All done, return program set.
                    return ProgramSet.Join(Op(nameof(Semantics.Ins)), kSpace, refSpace.Value, treeSpace.Value).Some();
                }

                case ResultKind.DELETE: // Use `Del`.
                {
                    // Synthesize param `ref`.
                    var refSpec = spec.MapOutputs((i, o) => ((Delete)i.errNode.context.result).oldNode);
                    var refSpace = LearnRef(refSpec, "Ins");
                    if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    // All done, return program set.
                    return ProgramSet.Join(Op(nameof(Semantics.Del)), refSpace.Value).Some();
                }
                
                case ResultKind.UPDATE: // Use `Upd`.
                {
                    // Synthesize param `ref`.
                    var refSpec = spec.MapOutputs((i, o) => ((Update)i.errNode.context.result).oldNode);
                    var refSpace = LearnRef(refSpec, "Upd");
                    if (!refSpace.HasValue || refSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    // Synthesize param `tree`.
                    var treeSpec = spec.MapOutputs((i, o) => ((Update)i.errNode.context.result).newNode);
                    var treeSpace = LearnTree(treeSpec, "Upd");
                    if (!treeSpace.HasValue || treeSpace.Value.IsEmpty)
                    {
                        return Optional<ProgramSet>.Nothing;
                    }

                    // All done, return program set.
                    return ProgramSet.Join(Op(nameof(Semantics.Upd)), refSpace.Value, treeSpace.Value).Some();
                }
            }

            return Optional<ProgramSet>.Nothing;
        }

        private Optional<ProgramSet> LearnRef(PremSpec<TInput, SyntaxNode> spec, string info = "")
        {
#if DEBUG
            Log.Fine("{0}.ref |- {1}", info, spec);
#endif
            // Case 1: error node = expected output, use `Just`.
            if (spec.Forall((i, o) => i.errNode == o))
            {
#if DEBUG
                Log.Fine("Just |- {1}", info, spec);
#endif
                return ProgramSet.List(Symbol(nameof(Semantics.Just)), Just()).Some();
            }

            // Case 2: expected output is an ancestor of error node, use `ancestor`.
            if (spec.Forall((i, o) => i.errNode.Ancestors().Contains(o)))
            {
#if DEBUG
                Log.Fine("ancestor |- {1}", info, spec);
#endif
                return LearnAncestor(spec);
            }

            // Case 3: expected output and error node are in different paths.
            if (spec.Forall((i, o) => !i.errNode.UpPath().Contains(o)))
            {
#if DEBUG
                Log.Fine("tree |- {1}", info, spec);
#endif
                return LearnFind(spec);
            }

            return Optional<ProgramSet>.Nothing;
        }

        private Optional<ProgramSet> LearnAncestor(PremSpec<TInput, SyntaxNode> spec)
        {
#if DEBUG
            Debug.Assert(spec.Forall((i, o) => i.errNode.Ancestors().Contains(o)));
            Log.Fine("Move.cursor |- {0}", spec);
#endif
            var cursors = new List<Cursor>();

            // Option 1: absolute cursor.
            int k = -1;
            if (spec.Identical((i, o) => o.depth - i.errNode.depth, out k))
            {
                cursors.Add(new AbsCursor(k));
            }

            // Option 2: relative cursor.
            Label label;
            if (spec.Identical((i, o) => o.label, out label) && 
                spec.Identical((i, o) => i.errNode.CountAncestorWhere(n => n.label.Equals(label), o.id), out k))
            {
                cursors.Add(new RelCursor(label, k));
            }

            if (cursors.Empty())
            {
                return Optional<ProgramSet>.Nothing;
            }
#if DEBUG
            ShowMany(cursors);
#endif
            return ProgramSet.List(Symbol(nameof(Semantics.Move)), cursors.Select(Move)).Some();
        }

        private Optional<ProgramSet> LearnFind(PremSpec<TInput, SyntaxNode> spec)
        {
            Debug.Assert(spec.Forall((i, o) => !i.errNode.UpPath().Contains(o)));
            return Optional<ProgramSet>.Nothing;
        }


        private Optional<ProgramSet> LearnTree(PremSpec<TInput, SyntaxNode> spec, string info = "")
        {
            return Optional<ProgramSet>.Nothing;
        }
    }
}

