using Optional;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Microsoft.ProgramSynthesis.Utils;
using System.Collections.Generic;

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

        public Optional<Leaf> FindLeafWhere(Func<Leaf, bool> predicate) =>
            root.GetSubtrees().TryFirst(n => n is Leaf && predicate((Leaf)n)).MaybeCast<Leaf>();

        private MultiValueDict<Record<Label, string>, int> _errFeatureDict;
        
        public IEnumerable<int> LocateErrFeatures(Label label, string token)
        {
            if (_errFeatureDict == null)
            {
                _errFeatureDict = new MultiValueDict<Record<Label, string>, int>();
                foreach (var p in err.FeatureChildren())
                {
                    foreach (var grp in p.child.Leaves().GroupBy(l => l.label))
                    {
                        // In this child, only leaf nodes with unique labels could be regarded as a feature.
                        if (grp.Count() == 1)
                        {
                            var grpLabel = grp.Key;
                            var grpToken = grp.AsEnumerable().First().code;
                            _errFeatureDict.Add(Record.Create(grpLabel, grpToken), p.index);
                        }
                    }
                }
            }

            return _errFeatureDict.GetValues(Record.Create(label, token));
        }
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