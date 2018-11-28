using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Prem.Util
{
    public class CST
    {
        public Node root { get; }

        private CST(string json)
        {
            var counter = new Counter();
            JObject obj = JObject.Parse(json);
            this.root = new Node(obj, 0, counter, this);
        }

        public static CST FromJSON(string json)
        {
            return new CST(json);
        }

        public Tree Get(int id)
        {
            return root.GetDescendants().Find(x => x.id == id);
        }

        public enum Kind
        {
            TOKEN, NODE, ERROR
        };

        abstract public class Tree
        {
            public Kind kind { get; }

            public int id { get; }

            public int depth { get; }

            public CST associatedTree { get; }

            public Tree(Kind kind, int depth, Counter counter, CST tree)
            {
                this.kind = kind;
                this.depth = depth;
                this.id = counter.AllocateId();
                this.associatedTree = tree;
            }

            public Node parent { get; set; }

            public bool HasParent()
            {
                return parent != null;
            }

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
            public List<Tree> GetAncestors()
            {
                var ancestors = new List<Tree>();
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

        public class Token : Tree
        {
            string value;

            Pos pos;

            public int type;

            public Token(JObject obj, int depth, Counter counter, CST tree) : 
                base(Kind.TOKEN, depth, counter, tree)
            {
                this.value = (string)obj["text"];
                this.pos = new Pos((int)obj["line"], (int)obj["pos"]);
                this.type = (int)obj["type"];
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
        }

        public class Error : Tree
        {
            string text;

            Pos pos;

            public Error(JObject obj, int depth, Counter counter, CST tree)
                : base(Kind.ERROR, depth, counter, tree)
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
        }

        public class Node : Tree
        {
            public string label { get; set; }

            public int arity { get; set; }

            public Tree[] children { get; set; }

            public Tree getChild(int i)
            {
                return children[i];
            }

            // BFS
            public List<Tree> GetDescendants()
            {
                var list = new List<Tree>();
                var queue = new Queue<Tree>();
                queue.Enqueue(this);

                while (queue.Any())
                {
                    Tree t = queue.Dequeue();
                    list.Add(t);

                    if (t.kind == Kind.NODE)
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

            public Node(JObject obj, int depth, Counter counter, CST tree)
                : base(Kind.NODE, depth, counter, tree)
            {
                this.label = (string)obj["label"];
                this.arity = (int)obj["arity"];
                this.children = new Tree[arity];

                JArray array = (JArray)obj["children"];
                for (int i = 0; i < arity; i++)
                {
                    JObject o = (JObject)array[i];
                    switch ((string)o["kind"])
                    {
                        case "node":
                            this.children[i] = new Node(o, depth + 1, counter, tree);
                            break;
                        case "leaf":
                            this.children[i] = new Token(o, depth + 1, counter, tree);
                            break;
                        case "error":
                            this.children[i] = new Error(o, depth + 1, counter, tree);
                            break;
                        default:
                            break;
                    }
                    this.children[i].parent = this;
                }
            }

            public override string ToString()
            {
                return $"({id}) {label}";
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
                if (label == that.label && arity == that.arity)
                {
                    var zip = children.Zip(that.children, (a, b) => a.Equals(b));
                    return zip.All(x => x);
                }
                return false;
            }
        }
    }

    public class Counter
    {
        private int _next_id = 0;

        public int AllocateId()
        {
            int id = _next_id;
            _next_id++;
            return id;
        }
    }
    
    public class Pos
    {
        int line;
        int offset;

        public Pos(int line, int pos)
        {
            this.line = line;
            this.offset = pos;
        }

        override public string ToString()
        {
            return $"({line}, {offset})";
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            }

            var that = (Pos) obj;
            return line == that.line && offset == that.offset;
        }
    }
}