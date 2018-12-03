using Optional;
using Newtonsoft.Json.Linq;
using System;

namespace Prem.Util
{
    public class SyntaxNodeContext
    {
        protected static Logger Log = Logger.Instance;

        public SyntaxNode root;

        protected Result _compareResult;

        protected Counter _counter;

        public SyntaxNodeContext()
        {
            _counter = new Counter();
        }

        public int AllocateId() => _counter.AllocateId();

        public SyntaxNodeContext(string json)
        {
            var counter = new Counter();
            
        }

        public static SyntaxNodeContext FromJSON(string json)
        {
            var context = new SyntaxNodeContext();
            JObject obj = JObject.Parse(json);
            context.root = Node.CreatePartialFromJSON(obj)(context, 0);

            return context;
        }

        public SyntaxNode FindNodeWhere(Predicate<SyntaxNode> predicate)
        {
            return root.GetDescendantsDFS().Find(predicate);
        }

        public SyntaxNode FindNode(int id)
        {
            return FindNodeWhere(x => x.id == id);
        }

        public void DoComparison(SyntaxNode target)
        {
            _compareResult = new SyntaxNodeComparer().GetResult(root, target);
            Log.Info("Compare result: {0}", _compareResult);
        }

        public Result GetResult() => _compareResult;
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
}