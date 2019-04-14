using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Transformer;
using Prem.Util;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
namespace Prem
{
    using Env = Dictionary<EnvKey, string>;

    public abstract class Matcher : ASTSerialization.IObjSerializable
    {
        public abstract bool Match(string word, Env env);

        public abstract void LabelVar(Counter counter);

        public abstract Type getSerializedType();

        public abstract XElement serialize();
    }

    public class Const : Matcher
    {
        public string literal { get; }

        public Const(string literal)
        {
            this.literal = literal;
        }

        public override bool Match(string word, Env env) => word == literal;

        public override void LabelVar(Counter counter) { }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Const)obj;
            return literal == that.literal;
        }

        public override int GetHashCode() => literal.GetHashCode();

        public override string ToString() => $"\"{literal}\"";

        public override Type getSerializedType() => typeof(Const);

        public override XElement serialize()
        {
            var xe = new XElement("Const");
            var literal_xe = new XElement("Attr-literal");
            ASTSerialization.Serialization.fillXElement(literal,literal_xe);
            xe.Add(literal_xe);
            return xe;
        }
        public Const(XElement xe)
        {
            literal = ASTSerialization.Serialization.makeObject(xe.Element("Attr-literal")) as string;
        }
    }

    public class Var : Matcher
    {
        public EnvKey var { get; private set; }

        public (string left, string right) quotePair { get; }

        public Var()
        {
            this.quotePair = NO_QUOTE;
        }

        public Var((string, string) quotePair)
        {
            this.quotePair = quotePair;
        }

        public static (string left, string right) NO_QUOTE = ("", "");

        public static List<(string left, string right)> QUOTE_PAIRS =
            new List<(string left, string right)> {
                ("'", "'"),
                ("`", "'"),
                ("\"", "\"")
            };

        public static (string left, string right) FindQuote(string word) =>
            QUOTE_PAIRS.TryFind(p => word.StartsWith(p.left) && word.EndsWith(p.right)).OrElse(NO_QUOTE);

        public static Optional<char> MatchedQuote(char left) =>
            QUOTE_PAIRS.TryFind(p => p.left.StartsWith(left)).Select(p => p.right.First());

        public override bool Match(string word, Env env)
        {
            var left = quotePair.left;
            var right = quotePair.right;
            if (!word.StartsWith(left) || !word.EndsWith(right)) return false;

            var raw = word.Substring(left.Length, word.Length - left.Length - right.Length);
            if (MatchSignature(raw, env))
            {
                return true;
            }

            env[var] = raw;
            return true;
        }

        public bool MatchSignature(string word, Env env)
        {
            var groups = word.Split('(', ')');
            if (groups.Length >= 2)
            {
                var fieldList = groups[0].Split('.');
                int count = 0;
                foreach (var field in fieldList.Reverse())
                {
                    env[var.Append(0, count)] = field;
                    count++;
                }

                if (groups[1].Any())
                {
                    var typeList = groups[1].Split(',');
                    count = 0;
                    foreach (var type in typeList)
                    {
                        env[var.Append(1, count)] = type;
                        count++;
                    }
                }

                return true;
            }

            return false;
        }

        public override void LabelVar(Counter counter)
        {
            this.var = new EnvKey(counter.AllocateId());
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Var)obj;
            return quotePair.left == that.quotePair.left && quotePair.right == that.quotePair.right;
        }

        public override int GetHashCode() => Hash.Combine(quotePair.left.GetHashCode(),
            quotePair.right.GetHashCode());

        public override string ToString() =>
            quotePair == NO_QUOTE ? $"?{var}" : $"{quotePair.left}?{var}{quotePair.right}";

        public override Type getSerializedType() => typeof(Var);

        public override XElement serialize()
        {
            var xe = new XElement("Var");
            var var_xe = new XElement("Attr-var");
            var quotePair_left_xe = new XElement("Attr-quotePair_left");
            var quotePair_right_xe = new XElement("Attr-quotePair_right");
            ASTSerialization.Serialization.fillXElement(var,var_xe);
            ASTSerialization.Serialization.fillXElement(quotePair.left,quotePair_left_xe);
            ASTSerialization.Serialization.fillXElement(quotePair.right,quotePair_right_xe);
            xe.Add(var_xe);
            xe.Add(quotePair_left_xe);
            xe.Add(quotePair_right_xe);
            return xe;
        }
        public Var(XElement xe)
        {
            var = ASTSerialization.Serialization.makeObject(xe.Element("Attr-var")) as EnvKey;
            quotePair = (ASTSerialization.Serialization.makeObject(xe.Element("Attr-quotePair_left")) as string,
                ASTSerialization.Serialization.makeObject(xe.Element("Attr-quotePair_right")) as string);
        }
    }

    public class ErrPattern : ASTSerialization.IObjSerializable
    {
        public List<Matcher> matchers { get; }

        public ErrPattern()
        {
            this.matchers = new List<Matcher>();
        }

        public ErrPattern(List<Matcher> matchers)
        {
            this.matchers = matchers;
        }

        public void Append(Matcher matcher)
        {
            this.matchers.Add(matcher);
        }

        public int Length
        {
            get => matchers.Count;
        }

        public static List<string> Tokenize(string message)
        {
            var words = new List<string>();

            var quoted = false;
            var sb = new StringBuilder();
            var end = ' ';
            for (var i = 0; i < message.Length; i++)
            {
                if (quoted)
                {
                    if (message[i] != ' ')
                    {
                        sb.Append(message[i]);
                        if (message[i] == end)
                        {
                            quoted = false;
                            words.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
                else
                {
                    if (message[i] == ' ')
                    {
                        if (sb.Length > 0)
                        {
                            words.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else
                    {
                        sb.Append(message[i]);

                        var match = Var.MatchedQuote(message[i]);
                        if (match.HasValue)
                        {
                            end = match.Value;
                            quoted = true;
                        }
                    }
                }
            }

            if (sb.Length > 0)
            {
                words.Add(sb.ToString());
            }

            return words;
        }

        public void LabelVars()
        {
            var counter = new Counter();
            this.matchers.ForEach(m => m.LabelVar(counter));
        }

        public bool Match(string message, Env env)
        {
            var words = Tokenize(message);
            if (matchers.Count != words.Count) return false;

            for (int i = 0; i < matchers.Count; i++)
            {
                if (!matchers[i].Match(words[i], env)) return false;
            }

            return true;
        }

        public ErrPattern Map2(ErrPattern pattern, Func<Matcher, Matcher, Matcher> func) =>
            new ErrPattern(matchers.Map2(pattern.matchers, func));

        public override string ToString() =>
            $"[{String.Join(", ", matchers.Select(m => m.ToString()))}]";

        public JObject DumpJSON()
        {
            var jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            var obj = JObject.Parse(JsonConvert.SerializeObject(this,jset));
            return obj;
        }

        // load from JSON
        public static ErrPattern FromJSON(JObject obj)
        {
            var jset = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };
            var pat = JsonConvert.DeserializeObject<ErrPattern>(obj.ToString(),jset);
            return pat;
        }

        public Type getSerializedType() => typeof(ErrPattern);

        public XElement serialize()
        {
            var xe = new XElement("ErrPattern");
            var matchers_xe = new XElement("Attr-matchers");
            ASTSerialization.Serialization.fillXElement(matchers,matchers_xe);
            xe.Add(matchers_xe);
            return xe;
        }

        public ErrPattern(XElement xe)
        {
            matchers = ASTSerialization.Serialization.makeObject(xe.Element("Attr-matchers")) as List<Matcher>;
        }
    }

    public class RuleSet : ASTSerialization.IObjSerializable
    {
        private static ColorLogger Log = ColorLogger.Instance;

        public ErrPattern errPattern { get; }

        public List<TProgram> transformers { get; }

        private RuleSet()
        {
            this.transformers = new List<TProgram>();
        }

        public RuleSet(ErrPattern pattern, List<TProgram> programs)
        {
            this.errPattern = pattern;
            this.transformers = programs;
        }

        public static RuleSet Empty = new RuleSet();

        public int Size => transformers.Count;

        public bool IsEmpty => !transformers.Any();

        public Optional<SyntaxNode> ApplyTop(Input input)
        {
            var env = new Env();
            if (!errPattern.Match(input.errMessage, env)) return Optional<SyntaxNode>.Nothing;

            return transformers.First().Apply(input.AsTInput(env));
        }

        public bool TestTop(Example example)
        {
            var env = new Env();
            if (!errPattern.Match(example.input.errMessage, env)) return false;

            var input = example.input.AsTInput(env);
            var expected = example.output.root;
            return transformers.First().Apply(input).Any(tree => tree.IdenticalTo(expected));
        }

        public IEnumerable<KeyValuePair<Example, bool>> TestTopMany(IEnumerable<Example> examples) =>
            examples.SelectPair(TestTop);

        public Optional<int> TestAll(Example example)
        {
            Log.Debug("Testing: {0}", example);

            var env = new Env();
            if (!errPattern.Match(example.input.errMessage, env))
            {
                Log.Warning("Pattern {0} cannot match against \"{1}\".", errPattern, example.input.errMessage);
                return Optional<int>.Nothing;
            }

            var input = example.input.AsTInput(env);
            var expected = example.output.root;
            return transformers.FirstCount(t =>
                t.Apply(input).Any(tree => tree.IdenticalTo(expected)));
        }

        public IEnumerable<KeyValuePair<Example, Optional<int>>> TestAllMany(IEnumerable<Example> examples) =>
            examples.SelectPair(TestAll);

        public JObject DumpJSON()
        {
            var obj = new JObject();
            obj.Add("errPattern",errPattern.DumpJSON());
            List<JObject> listJObject = new List<JObject>();
            foreach(var transformer in transformers)
            {
                listJObject.Add(transformer.DumpJSON());
            }
            obj.Add("transformers",JToken.FromObject(listJObject));
            return obj;
        }

        // load from JSON
        public static RuleSet FromJSON(JObject obj)
        {
            var pattern = ErrPattern.FromJSON((JObject) obj["errPattern"]);
            List<TProgram> programs = new List<TProgram>();
            foreach(var program in obj["transformers"].ToObject<List<JObject>>())
            {
                programs.Add(TProgram.FromJSON(program));
            }
            var ruleSet = new RuleSet(pattern,programs);
            return ruleSet;
        }

        public Type getSerializedType() => typeof(RuleSet);
        public XElement serialize()
        {
            var xe = new XElement("RuleSet");
            var errPattern_xe = new XElement("Attr-errPattern");
            ASTSerialization.Serialization.fillXElement(errPattern,errPattern_xe);
            var transformers_xe = new XElement("Attr-transformers");
            ASTSerialization.Serialization.fillXElement(transformers,transformers_xe);
            xe.Add(errPattern_xe);
            xe.Add(transformers_xe);
            return xe;
        }
        
        public RuleSet(XElement xe)
        {
            errPattern = ASTSerialization.Serialization.makeObject(xe.Element("Attr-errPattern")) as ErrPattern;
            transformers = ASTSerialization.Serialization.makeObject(xe.Element("Attr-transformers")) as List<TProgram>;
        }
    }

    public class RuleLib : ASTSerialization.IObjSerializable
    {
        public List<RuleSet> ruleSets{get;}
        public RuleLib()
        {
            this.ruleSets = new List<RuleSet>();
        }
        public RuleLib(List<RuleSet> ruleSets)
        {
            this.ruleSets = ruleSets;
        }
        public int Size => ruleSets.Count;

        public bool IsEmpty => !ruleSets.Any();

        public void add(RuleSet ruleset)
        {
            this.ruleSets.Add(ruleset);
        }

        public JObject DumpJSON()
        {
            var obj = new JObject();
            List<JObject> listJObject = new List<JObject>();
            foreach(var ruleSet in ruleSets)
            {
                listJObject.Add(ruleSet.DumpJSON());
            }
            obj.Add("ruleSets",JToken.FromObject(listJObject));
            return obj;
        }
        
        public static RuleLib FromJSON(JObject obj)
        {
            List<RuleSet> ruleSets = new List<RuleSet>();
            var x = obj["ruleSets"];
            foreach(var ruleSet in x.ToObject<List<JObject>>())
            {
                ruleSets.Add(RuleSet.FromJSON(ruleSet));
            }
            return new RuleLib(ruleSets);
        }

        public XElement DumpXml() => serialize();
        public static RuleLib FromXml(XElement xe) => new RuleLib(xe);

        public RuleLib(XElement xe)
        {
            ruleSets = ASTSerialization.Serialization.makeObject(xe.Element("Attr-ruleSets")) as List<RuleSet>;
        }

        public XElement serialize()
        {
            var xe = new XElement("RuleLib");
            var list_xe = new XElement("Attr-ruleSets");
            ASTSerialization.Serialization.fillXElement(ruleSets,list_xe);
            xe.Add(list_xe);
            return xe;
        }
        public Type getSerializedType() => typeof(RuleLib);
    }
}
