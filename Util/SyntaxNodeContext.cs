using Optional;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

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
            context.root = Node.CreatePartialFromJSON(obj).Instantiate(context, 0);

            return context;
        }

        public SyntaxNode FindNodeWhere(Func<SyntaxNode, bool> predicate) =>
            root.GetSubtrees().First(predicate);

        public Token FindTokenWhere(Func<Token, bool> predicate) =>
            FindNodeWhere(n => n.kind == SyntaxKind.TOKEN && predicate((Token)n)) as Token;

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