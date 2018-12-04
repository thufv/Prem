using System;
using System.Collections.Generic;

using Prem.Transformer;
using Prem.Util;

namespace Prem
{
    public abstract class Matcher
    {
        public bool isConst { get; }

        public Matcher(bool isConst)
        {
            this.isConst = isConst;
        }

        public abstract void Match(string word, Env<string> env);
    }

    public class ErrPattern
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

        public void Match(List<string> words, Env<string> env) =>
            matchers.ForEach2(words, (matcher, word) => matcher.Match(word, env));

        public void Append(Matcher matcher)
        {
            this.matchers.Add(matcher);
        }

        public int Length
        {
            get => matchers.Count;
        }

        public ErrPattern Map2(ErrPattern pattern, Func<Matcher, Matcher, Matcher> func) =>
            new ErrPattern(matchers.Map2(pattern.matchers, func));
    }

    public class RuleSet
    {
        public RuleSet()
        {

        }

        public RuleSet(ErrPattern pattern, List<TProgram> transformers)
        {

        }

        public static RuleSet Empty() => new RuleSet();
    }
}