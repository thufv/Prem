using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using Optional;

namespace Prem.Util
{
    using PartialFunc = Func<SyntaxNodeContext, int, SyntaxNode>;

    /// <summary>
    /// The `SyntaxNode` declared below is a concrete syntax node,
    /// say it must be associated with a context (which stores some global information of the tree)
    /// and initialized with an integer representing its depth/level in the tree.
    /// Building a tree in a top-down manner best suits this situation, 
    /// as we can increase the depth of a node throughout the entire process, recursively.
    /// 
    /// However, there are situations where trees must be built bottom-up.
    /// In this way, depths for each node are unknown until the entire tree completes the process.
    /// To achieve this, we introduce a `PartialNode`,
    /// which only stores the information of the node itself and its children,
    /// but not the depth and the associated tree context.
    /// 
    /// A partial node contains a partial function:
    /// once it takes a context and a depth as parameters, then it becomes a concrete syntax node.
    /// To instantiate a partial node, a top-down construction has to be processed.
    /// The instantiation process is realized as the constructors, as we see later.
    /// </summary>
    /// <returns>The partial node.</returns>
    public class PartialNode
    {
        public SyntaxNode orig { get; }

        private PartialFunc func;

        public PartialNode(SyntaxNode original, PartialFunc func)
        {
            this.orig = original;
            this.func = func;
        }

        public SyntaxNode Instantiate(SyntaxNodeContext context, int depth) => func(context, depth);

        public override string ToString() => orig == null ? "<PartialNode>" : $"<{orig}>";
    }

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

        public bool isLeaf { get; }

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
        /// Node label.
        /// In a syntax tree, a label could signify a nonterminal symbol (for `Node`),
        /// a terminal symbol (for `Token`), 
        /// or a special symbol indicating syntax error (for `Error`).
        /// 
        /// For a specified programming language, each label must be mapped into a unique id.
        /// </summary>
        /// <value>The label.</value>
        public Label label { get; }

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

        public SyntaxNode left;
        public SyntaxNode right;

        /// <summary>
        /// Transform a concrete node to a partial node, by removing the context and depth.
        /// </summary>
        /// <returns>The corresponding partial node.</returns>
        public abstract PartialNode ToPartial();

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
            Label label, string code = "")
        {
            this.kind = kind;
            this.isLeaf = kind != SyntaxKind.NODE;
            this.context = context;
            this.depth = depth;
            this.label = label;
            this.id = context.AllocateId();
            this.code = code;
            this.matches = new List<SyntaxNode>();
        }

        public IEnumerable<SyntaxNode> GetChildren() =>
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

        public IEnumerable<SyntaxNode> GetSubtrees() => DFS<SyntaxNode>(x => x);

        abstract public List<T> DFS<T>(Func<SyntaxNode, T> visit);

        // including itself        
        public IEnumerable<SyntaxNode> GetAncestors()
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

    public abstract class Leaf : SyntaxNode
    {
        public Pos pos { get; }

        public Leaf(SyntaxKind kind, SyntaxNodeContext context, 
            int depth, Label label, string code, Pos pos)
            : base(kind, context, depth, label, code)
        {
            this.pos = pos;
            this.treeHash = Hash.Combine(label.GetHashCode(), code.GetHashCode());
        }

        public override List<T> DFS<T>(Func<SyntaxNode, T> visitor)
        {
            return new List<T> { visitor(this) };
        }

        public override string ToString() => $"({id}) {label} \"{code}\" @ {pos}";

        public override void PrintTo(IndentPrinter printer)
        {
            printer.Print(ToString());
            printer.PrintLine($" #{treeHash}");
        }
    }

    public class Token : Leaf
    {
        public Token(SyntaxNodeContext context, int depth, Label label, string code, Pos pos)
            : base(SyntaxKind.TOKEN, context, depth, label, code, pos)
        {
        }

        public override PartialNode ToPartial() => new PartialNode(this,
            (context, depth) => new Token(context, depth, label, code, pos));

        /// <summary>
        /// This group of functions can be regarded as the constructors of partial nodes,
        /// which takes necessary information and returns a partial node.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="code"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static PartialNode CreatePartial(Label label, string code, Pos pos = null) =>
            new PartialNode(null, (context, depth) => new Token(context, depth, label, code, pos));

        /// <summary>
        /// This group of functions are handy constructors of partial nodes,
        /// extracting necessary information from a JSON object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static PartialNode CreatePartialFromJSON(JObject obj)
        {
            var label = new Label((int)obj["label"], (string)obj["name"]);
            var code = (string)obj["code"];
            var pos = new Pos((int)obj["line"], (int)obj["pos"]);

            return CreatePartial(label, code, pos);
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            return that.kind == SyntaxKind.TOKEN && that.label.Equals(label) && that.code == code;
        }
    }

    public class Error : Leaf
    {
        public Error(SyntaxNodeContext context, int depth, Label label, string code, Pos pos)
            : base(SyntaxKind.ERROR, context, depth, label, code, pos)
        {
        }

        public override PartialNode ToPartial() => new PartialNode(this,
            (context, depth) => new Error(context, depth, label, code, pos));

        public static PartialNode CreatePartial(Label label, string code, Pos pos) =>
            new PartialNode(null, (context, depth) => new Error(context, depth, label, code, pos));

        public static PartialNode CreatePartialFromJSON(JObject obj)
        {
            var label = new Label((int)obj["label"], "ERROR");
            var code = (string)obj["code"];
            var pos = new Pos((int)obj["line"], (int)obj["pos"]);

            return CreatePartial(label, code, pos);
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            return that.kind == SyntaxKind.ERROR && that.label.Equals(label) && that.code == code;
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

        public Node(SyntaxNodeContext context, int depth, Label label,
            IEnumerable<PartialNode> children, string code = "")
            : base(SyntaxKind.NODE, context, depth, label, code)
        {
            this.children = children.Select(t => t.Instantiate(context, depth + 1)).ToList();
            this.children.ForEach(t => t.parent = this);
            this.treeHash = Hash.Combine(label.GetHashCode(), 
                this.children.Select(t => t.treeHash));
        }

        public override PartialNode ToPartial() => new PartialNode(this,
            (context, depth) => new Node(context, depth, label, 
                children.Select(t => t.ToPartial())));

        public static PartialNode CreatePartial(Label label, 
            IEnumerable<PartialNode> builders, string code = "") => new PartialNode(null,
            (context, depth) => new Node(context, depth, label, builders, code));

        public static PartialNode CreatePartialFromJSON(JObject obj)
        {
            var label = new Label((int)obj["label"], (string)obj["name"]);
            var code = (string)obj["code"];
            var builders = obj["children"]
                .Select(t => (JObject)t)
                .Select(o =>
                    {
                        var kind = (string)o["kind"];
                        return kind == "node" ? Node.CreatePartialFromJSON(o)
                            : kind == "leaf" ? Token.CreatePartialFromJSON(o)
                            : Error.CreatePartialFromJSON(o);
                    });

            return CreatePartial(label, builders, code);
        }

        public override int GetNumChildren() => children.Count;

        public override List<T> DFS<T>(Func<SyntaxNode, T> visit)
        {
            var results = new List<T> { visit(this) };
            children.ForEach(x => results.AddRange(x.DFS(visit)));
            return results;
        }

        public override bool IdenticalTo(SyntaxNode that)
        {
            if (that.kind != SyntaxKind.NODE || !that.label.Equals(label) || 
                that.GetNumChildren() != GetNumChildren())
            {
                return false;
            }

            return GetChildren().Zip(that.GetChildren(), (x, y) => x.IdenticalTo(y)).All(b => b);
        }

        public override string ToString() => $"({id}) {label}";

        public override void PrintTo(IndentPrinter printer)
        {
            printer.Print(ToString());
            printer.PrintLine($" #{treeHash}");
            printer.IncIndent();
            children.ForEach(x => x.PrintTo(printer));
            printer.DecIndent();
        }
    }
}