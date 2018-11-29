using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;

namespace Prem.Util
{
    public enum SyntaxKind
    {
        TOKEN,
        NODE,
        ERROR
    }

    abstract public class SyntaxNode
    {
        private static Logger Log = Logger.Instance;

        public SyntaxKind kind { get; }

        public int id { get; }

        public int depth { get; }

        public SyntaxNodeContext context { get; }

        public string code { get; }

        public TextSpan span { get; }

        public int label { get; }

        public Node parent { get; set; }

        public bool HasParent()
        {
            return parent != null;
        }

        public SyntaxNode(SyntaxKind kind, JObject obj,
            int depth, Counter counter, SyntaxNodeContext context)
        {
            this.kind = kind;
            this.id = counter.AllocateId();
            this.depth = depth;
            this.context = context;
            this.code = (string)obj["code"];
            // TODO: span
            this.label = (int)obj["label"];
        }

        public List<SyntaxNode> GetChildrenOrNull() =>
            kind == SyntaxKind.NODE ? ((Node)this).children.ToList() : null;

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

        public Token(JObject obj, int depth, Counter counter, SyntaxNodeContext context) :
            base(SyntaxKind.TOKEN, obj, depth, counter, context)
        {
            this.type = label;
            this.value = code;
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
    }

    public class Error : SyntaxNode
    {
        string text;

        Pos pos;

        public Error(JObject obj, int depth, Counter counter, SyntaxNodeContext context)
            : base(SyntaxKind.ERROR, obj, depth, counter, context)
        {
            this.text = (string)obj["text"];
            this.pos = new Pos((int)obj["line"], (int)obj["pos"]);
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
    }

    public class Node : SyntaxNode
    {
        public string name { get; set; }

        public int arity { get; set; }

        public SyntaxNode[] children { get; set; }

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

        public Node(JObject obj, int depth, Counter counter, SyntaxNodeContext context)
            : base(SyntaxKind.NODE, obj, depth, counter, context)
        {
            this.name = (string)obj["name"];
            this.arity = (int)obj["arity"];
            this.children = new SyntaxNode[arity];

            JArray array = (JArray)obj["children"];
            for (int i = 0; i < arity; i++)
            {
                JObject o = (JObject)array[i];
                switch ((string)o["kind"])
                {
                    case "node":
                        this.children[i] = new Node(o, depth + 1, counter, context);
                        break;
                    case "leaf":
                        this.children[i] = new Token(o, depth + 1, counter, context);
                        break;
                    case "error":
                        this.children[i] = new Error(o, depth + 1, counter, context);
                        break;
                    default:
                        break;
                }
                this.children[i].parent = this;
            }
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
    }
}