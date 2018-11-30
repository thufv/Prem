using Optional;
using Newtonsoft.Json.Linq;

namespace Prem.Util
{
    public class SyntaxNodeContext
    {
        public SyntaxNode root { get; set; }

        protected Counter counter;

        public SyntaxNodeContext()
        {
            counter = new Counter();
        }

        public int AllocateId() => counter.AllocateId();

        public SyntaxNodeContext(string json)
        {
            var counter = new Counter();
            
        }

        public Option<Result> diffResult { get; set; }

        public static SyntaxNodeContext FromJSON(string json)
        {
            var context = new SyntaxNodeContext();
            JObject obj = JObject.Parse(json);
            context.root = Node.JSONBuilder(obj)(context, 0);
            context.diffResult = Option.None<Result>();

            return context;
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
}