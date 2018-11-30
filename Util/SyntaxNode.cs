using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

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
        private static Logger Log = Logger.Instance;

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

        public int GetNumChildren() => kind == SyntaxKind.NODE ? ((Node)this).arity : 0;

        public List<SyntaxNode> GetDescendantsDFS()
        {
            var nodes = DFS<SyntaxNode>(x => x);
            nodes.RemoveAt(0);
            Log.Debug("des for {0}: {1}", this, Show.L(nodes));
            return nodes;
        }

        abstract public List<T> DFS<T>(Func<SyntaxNode, T> visit);

        public Node GetAncestorWhere(Func<Node, bool> predicate, int k)
        {
            Node node = parent;
            while (true)
            {
                if (predicate(node))
                {
                    if (k == 0) return node;
                    k--;
                }

                if (node.HasParent())
                {
                    node = node.parent;
                }
                else
                {
                    return null;
                }
            }
        }

        public Node GetAncestor(int k)
        {
            return GetAncestorWhere(_ => true, k);
        }

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

        public int CountAncestorWhere(Func<Node, bool> predicate, int until)
        {
            Node node = parent;
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

        abstract public void PrintTo(IndentPrinter printer);
    }

    public class Token : SyntaxNode
    {
        // TODO: DEPRECATED
        public string value { get; }

        Pos pos;

        // TODO: DEPRECATED
        public int type { get; }

        public Token(SyntaxNodeContext context, int depth, int label, string name, string code = "")
            : base(SyntaxKind.TOKEN, context, depth, label, name, code)
        {
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];
                var name = (string)obj["name"];

                return new Token(context, depth, label, name);
            };
        }

        public override string ToString()
        {
            return $"({id}) {value} : {type} @ {pos}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine(ToString());
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Token)obj;
            return type == that.type && value == that.value;
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visitor)
        {
            return new List<T> { visitor(this) };
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Token(context, depth, label, name);
            };
        }
    }

    public class Error : SyntaxNode
    {
        string text;

        Pos pos;

        public Error(SyntaxNodeContext context, int depth, int label)
            : base(SyntaxKind.ERROR, context, depth, label, "ERROR")
        {
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];

                return new Error(context, depth, label);
            };
        }

        public override string ToString()
        {
            return $"({id}) {text} : <error> @ {pos}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine(ToString());
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Error)obj;
            return text == that.text;
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visitor)
        {
            return new List<T> { visitor(this) };
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Token(context, depth, label, name);
            };
        }
    }

    public class Node : SyntaxNode
    {
        public string name { get; set; }

        public int arity { get; set; }

        public List<SyntaxNode> children { get; set; }

        public SyntaxNode getChild(int i)
        {
            return children[i];
        }

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
            List<Func<SyntaxNodeContext, int, SyntaxNode>> builders) 
            : base(SyntaxKind.NODE, context, depth, label, name)
        {
            this.arity = builders.Count;
            this.children = builders.Select(f => f(context, depth + 1)).ToList();
        }

        public override string ToString()
        {
            return $"({id}) {name}";
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine(ToString());
            printer.IncIndent();
            for (int i = 0; i < arity; i++)
            {
                children[i].PrintTo(printer);
            }
            printer.DecIndent();
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Node)obj;
            if (name == that.name && arity == that.arity)
            {
                var zip = children.Zip(that.children, (a, b) => a.Equals(b));
                return zip.All(x => x);
            }
            return false;
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visit)
        {
            var results = new List<T> { visit(this) };
            for (int i = 0; i < arity; i++)
            {
                results.AddRange(children[i].DFS(visit));
            }
            return results;
        }

        public override Func<SyntaxNodeContext, int, SyntaxNode> CloneBuilder()
        {
            return (context, depth) =>
            {
                return new Node(context, depth, label, name, 
                    children.Select(x => x.CloneBuilder()).ToList());
            };
        }

        public static Func<SyntaxNodeContext, int, SyntaxNode> JSONBuilder(JObject obj)
        {
            return (context, depth) =>
            {
                var label = (int)obj["label"];
                var name = (string)obj["name"];
                var builders = obj["children"].Select(t => (JObject)t)
                    .Select(o =>
                    {
                        var kind = (string)o["kind"];
                        return kind == "node" ? Node.JSONBuilder(o)
                            : kind == "leaf" ? Token.JSONBuilder(o)
                            : Error.JSONBuilder(o);
                    }).ToList();

                return new Node(context, depth, label, name, builders);
            };
        }
    }
}