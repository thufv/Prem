using System;
using Newtonsoft.Json.Linq;

namespace Prem.Util
{
    abstract public class CST
    {
        public enum Kind
        {
            LEAF, NODE, ERROR
        };

        public Kind kind { get; }

        public int id { get; }

        private static int _next_id;

        public static CST[] NodeList = new CST[1024];

        public CST(Kind kind)
        {
            this.kind = kind;
            this.id = _next_id;
            NodeList[id] = this;
            _next_id++;
        }

        public static CSTNode FromJSON(string json)
        {
            _next_id = 0;
            JObject obj = JObject.Parse(json);
            return new CSTNode(obj);
        }

        public CSTNode parent { get; set; }

        public bool HasParent()
        {
            return parent != null;
        }

        public CSTNode GetAncestorWhere(Func<CSTNode, bool> predicate, int k)
        {
            CSTNode node = parent;
            while (true) {
                if (predicate(node)) {
                    k--;
                    if (k == 0) return node;
                }

                if (node.HasParent()) {
                    node = node.parent;
                } else {
                    return null;
                }
            }
        }

        public CSTNode GetAncestor(int k)
        {
            return GetAncestorWhere(_ => true, k);
        }

        abstract public void PrintTo(IndentPrinter printer);
    }

    public class CSTLeaf : CST
    {
        string value;
        
        Pos pos;

        public int type;

        public CSTLeaf(JObject obj) : base(Kind.LEAF)
        {
            this.value = (string) obj["text"];
            this.pos = new Pos((int) obj["line"], (int)obj["pos"]);
            this.type = (int) obj["type"];
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine($"({id}) {value} : {type} @ {pos}");
        }
    }

    public class CSTError : CST
    {
        string text;
        
        Pos pos;

        public CSTError(JObject obj) : base(Kind.ERROR)
        {
            this.text = (string)obj["text"];
            this.pos = new Pos((int)obj["line"], (int)obj["pos"]);
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine($"({id}) {text} : <error> @ {pos}");
        }
    }

    public class CSTNode : CST
    {
        public string label { get; set; }
        
        public int arity { get; set; }

        public CST[] children { get; set; }

        public CST getChild(int i)
        {
            return children[i];
        }

        public CSTNode(JObject obj) : base(Kind.NODE)
        {
            this.label = (string) obj["label"];
            this.arity = (int) obj["arity"];
            this.children = new CST[arity];

            JArray array = (JArray) obj["children"];
            for (int i = 0; i < arity; i++) {
                JObject o = (JObject) array[i];
                switch ((string) o["kind"]) {
                    case "node":
                        this.children[i] = new CSTNode(o);
                        break;
                    case "leaf":
                        this.children[i] = new CSTLeaf(o);
                        break;
                    case "error":
                        this.children[i] = new CSTError(o);
                        break;
                    default:
                        break;
                }
                this.children[i].parent = this;
            }
        }

        public override void PrintTo(IndentPrinter printer)
        {
            printer.PrintLine($"({id}) {label}");
            printer.IncIndent();
            for (int i = 0; i < arity; i++) {
                children[i].PrintTo(printer);
            }
            printer.DecIndent();
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
    }
}