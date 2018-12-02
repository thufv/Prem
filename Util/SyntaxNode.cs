using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Optional;

namespace Prem.Util
{
    public enum SyntaxKind
    {
        TOKEN,
        NODE,
        ERROR
    }

    /// <summary>
    /// A tree structure representing a concrete syntax tree.
    /// `Node` is an internal node,
    /// `Token` and `Error` are leaf nodes.
    /// </summary>
    abstract public class SyntaxNode
    {
        protected static Logger Log = Logger.Instance;

        public SyntaxKind kind { get; }

        /// <summary>
        /// Associated tree context, which stores useful information about the entire tree.
        /// </summary>
        /// <value>The tree context.</value>
        public SyntaxNodeContext context { get; }

        /// <summary>
        /// Unique identifier (inside the tree).
        /// </summary>
        /// <value>The identifier.</value>
        public int id { get; }

        /// <summary>
        /// Node depth, counting from 0 (the root level).
        /// </summary>
        /// <value>The depth.</value>
        public int depth { get; }

        /// <summary>
        /// Node label id.
        /// In a syntax tree, a label could signify a nonterminal symbol (for `Node`),
        /// a terminal symbol (for `Token`), 
        /// or a special symbol indicating syntax error (for `Error`).
        /// 
        /// For a specified programming language, each label must be mapped into a unique id.
        /// </summary>
        /// <value>The label id.</value>
        public int label { get; }

        /// <summary>
        /// Node label name, for humans to read.
        /// </summary>
        /// <value>The name for the `label`.</value>
        public string name { get; }

        /// <summary>
        /// A string representing the source code fragment of the node.
        /// </summary>
        /// <value>The source code.</value>
        public string code { get; }

        /// <summary>
        /// A hash value for the entire tree/subtree.
        /// </summary>
        /// <value>The tree hash value.</value>
        public int treeHash { get; protected set; }

        /// <summary>
        /// Parent node.
        /// </summary>
        /// <value>The parent node, null if `this` is the root.</value>
        public Node parent { get; set; }

        public bool HasParent()
        {
            return parent != null;
        }

        /// <summary>
        /// Building a tree is not easy: we have to handle a couple of things carefully,
        /// including allocating ids, computing depths and associating parents.
        /// Instead of specifying complex constructors, we use a more flexible way, 
        /// namely a builder, which is actually a function of type 
        /// `SyntaxNodeContext * int -> SyntaxNode`.
        /// It takes a context and depth as input, and returns a node as the constrution result.
        /// 
        /// A clone builder is a builder where it simply returns a copy of `this` node.
        /// </summary>
        /// <returns>The clone builder.</returns>
        public abstract Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder();

        /// <summary>
        /// Internal base constructor.
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="context"></param>
        /// <param name="depth"></param>
        /// <param name="label"></param>
        /// <param name="name"></param>
        /// <param name="code"></param>
        protected SyntaxNode(SyntaxKind kind, SyntaxNodeContext context, int depth, 
            int label, string name, string code = "")
        {
            this.kind = kind;
            this.context = context;
            this.depth = depth;
            this.label = label;
            this.name = name;
            this.id = context.AllocateId();
            this.code = code;
        }

        public SyntaxNode AddChild(int k)
        {
            return null;
        }

        public List<SyntaxNode> GetChildren() =>
            kind == SyntaxKind.NODE ? ((Node)this).children.ToList() : new List<SyntaxNode>();

        public virtual int GetNumChildren() => 0;

        public Option<SyntaxNode> GetAncestorWhere(Predicate<SyntaxNode> predicate, int k)
        {
            Debug.Assert(k > 0);
            SyntaxNode node = this;
            
            while (true)
            {
                if (node.parent != null)
                {
                    node = node.parent;
                }
                else
                {
                    return Option.None<SyntaxNode>();
                }

                if (predicate(node))
                {
                    if (k == 1) return Option.Some<SyntaxNode>(node);
                    k--;
                }
            }
        }

        public Option<SyntaxNode> GetAncestor(int k)
        {
            return GetAncestorWhere(_ => true, k);
        }

        public List<SyntaxNode> GetDescendantsDFS()
        {
            var nodes = DFS<SyntaxNode>(x => x);
            nodes.RemoveAt(0);
            Log.Debug("des for {0}: {1}", this, Show.L(nodes));
            return nodes;
        }

        public List<SyntaxNode> GetSubtrees()
        {
            return DFS<SyntaxNode>(x => x);
        }

        abstract public List<T> DFS<T>(Func<SyntaxNode, T> visit);

        // including itself        
        public List<SyntaxNode> GetAncestors()
        {
            var ancestors = new List<SyntaxNode>();
            var node = this;
            ancestors.Add(node);

            while (node.HasParent())
            {
                node = node.parent;
                ancestors.Add(node);
            }

            return ancestors;
        }

        public int CountAncestorWhere(Func<SyntaxNode, bool> predicate, int until)
        {
            var node = this;
            int count = 0;
            while (true)
            {
                if (predicate(node))
                {
                    count++;
                    if (node.id == until) return count;
                }

                if (node.HasParent())
                {
                    node = node.parent;
                }
            }
        }

        public List<SyntaxNode> matches { get; set; }

        public abstract bool IdenticalTo(SyntaxNode that);

        abstract public void PrintTo(IndentPrinter printer);
    }

    public class Token : SyntaxNode
    {
        public Pos pos { get; }

        public Token(SyntaxNodeContext context, int depth, int label, string name, string code,
            Pos pos)
            : base(SyntaxKind.TOKEN, context, depth, label, name, code)
        {
            this.pos = pos;
            this.treeHash = Hash.Combine(name.GetHashCode(), code.GetHashCode());
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];
                var name = (string)obj["name"];
                var code = (string)obj["code"];
                var line = (int)obj["line"];
                var pos = (int)obj["pos"];

                return new Token(context, depth, label, name, code, new Pos(line, pos));
            };
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visitor)
        {
            return new List<T> { visitor(this) };
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Token(context, depth, label, name, code, pos);
            };
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            return that.kind == SyntaxKind.TOKEN && label == that.label && code == that.code;
        }

        public override string ToString()
        {
            return $"({id}) {name} \"{code}\" @ {pos}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.Print(ToString());
            printer.PrintLine($" <{treeHash}>");
        }
    }

    public class Error : SyntaxNode
    {
        Pos pos;

        public Error(SyntaxNodeContext context, int depth, int label, string code, Pos pos)
            : base(SyntaxKind.ERROR, context, depth, label, "<error>", code)
        {
            this.pos = pos;
            this.treeHash = Hash.Combine(name.GetHashCode(), code.GetHashCode());
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];
                var code = (string)obj["code"];
                var line = (int)obj["line"];
                var offset = (int)obj["pos"];

                return new Error(context, depth, label, code, new Pos(line, offset));
            };
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visitor)
        {
            return new List<T> { visitor(this) };
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Error(context, depth, label, code, pos);
            };
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            return that.kind == SyntaxKind.ERROR && label == that.label && code == that.code;
        }

        public override string ToString()
        {
            return $"({id}) {name} \"{code}\" @ {pos}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.Print(ToString());
            printer.PrintLine($" <{treeHash}>");
        }
    }

    public class Node : SyntaxNode
    {
        public List<SyntaxNode> children { get; set; }

        public SyntaxNode GetChild(int i) => children[i];

        // BFS
        public List<SyntaxNode> GetDescendants()
        {
            var list = new List<SyntaxNode>();
            var queue = new Queue<SyntaxNode>();
            queue.Enqueue(this);

            while (queue.Any())
            {
                SyntaxNode t = queue.Dequeue();
                list.Add(t);

                if (t.kind == SyntaxKind.NODE)
                {
                    var node = (Node)t;
                    foreach (var child in node.children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return list;
        }

        public Node(SyntaxNodeContext context, int depth, int label, string name,
            IEnumerable<Func<SyntaxNodeContext, int, SyntaxNode>> builders, string code = "")
            : base(SyntaxKind.NODE, context, depth, label, name, code)
        {
            this.children = builders.Select(f => f(context, depth + 1)).ToList();
            this.children.ForEach(x => x.parent = this);
            this.treeHash = Hash.Combine(name.GetHashCode(), children.Select(x => x.treeHash));
        }

        public override int GetNumChildren() => children.Count;

        public override List<T> DFS<T>(Func<SyntaxNode, T> visit)
        {
            var results = new List<T> { visit(this) };
            children.ForEach(x => results.AddRange(x.DFS(visit)));
            return results;
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Node(context, depth, label, name,
                    children.Select(x => x.CloneBuilder()));
            };
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];
                var name = (string)obj["name"];
                var code = (string)obj["code"];
                var builders = obj["children"]
                    .Select(t => (JObject)t)
                    .Select(o =>
                        {
                            var kind = (string)o["kind"];
                            return kind == "node" ? Node.JSONBuilder(o)
                                : kind == "leaf" ? Token.JSONBuilder(o)
                                : Error.JSONBuilder(o);
                        });

                return new Node(context, depth, label, name, builders, code);
            };
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            if (that.kind != SyntaxKind.NODE || GetNumChildren() != that.GetNumChildren()) 
            {
                return false;
            }

            return GetChildren().Zip(that.GetChildren(), (x, y) => x.IdenticalTo(y)).All(b => b);
        }

        public override string ToString()
        {
            return $"({id}) {name}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.Print(ToString());
            printer.PrintLine($" <{treeHash}>");
            printer.IncIndent();
            children.ForEach(x => x.PrintTo(printer));
            printer.DecIndent();
        }
    }
}