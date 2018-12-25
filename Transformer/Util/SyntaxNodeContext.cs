using Optional;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Prem.Util
{
    public class SyntaxNodeContext
    {
        protected static ColorLogger Log = ColorLogger.Instance;

        public SyntaxNode root;

        public Leaf err { get; set; }

        public Result result { get; set; }

        protected Counter _counter;

        public SyntaxNodeContext()
        {
            _counter = new Counter();
        }

        public int AllocateId() => _counter.AllocateId();

        public static SyntaxNodeContext FromJSON(string json)
        {
            var context = new SyntaxNodeContext();
            var obj = JObject.Parse(json);
            context.root = Node.CreatePartialFromJSON(obj).Instantiate(context, 0);

            return context;
        }

        public Option<SyntaxNode> FindNodeWhere(Func<SyntaxNode, bool> predicate)
        {
            var node = root.GetSubtrees().FirstOrDefault(predicate);
            return node == null ? Option.None<SyntaxNode>() : Option.Some<SyntaxNode>(node);
        }

        public Option<SyntaxNode> FindLeafWhere(Func<Leaf, bool> predicate) =>
            FindNodeWhere(n => n.isLeaf && predicate((Leaf)n));
    }

    public class Counter
    {
        private int _next_id = 0;

        public int AllocateId()
        {
            var id = _next_id;
            _next_id++;
            return id;
        }
    }
}