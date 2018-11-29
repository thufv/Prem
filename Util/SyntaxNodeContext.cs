using Optional;
using Newtonsoft.Json.Linq;

namespace Prem.Util
{
    public class SyntaxNodeContext
    {
        public Node root { get; }

        private SyntaxNodeContext(string json)
        {
            var counter = new Counter();
            JObject obj = JObject.Parse(json);
            this.root = new Node(obj, 0, counter, this);
            this.diffResult = Option.None<DiffResult>();
        }

        public Option<DiffResult> diffResult { get; set; }

        public static SyntaxNodeContext FromJSON(string json)
        {
            return new SyntaxNodeContext(json);
        }

        public SyntaxNode Get(int id)
        {
            return root.GetDescendants().Find(x => x.id == id);
        }
    }
}