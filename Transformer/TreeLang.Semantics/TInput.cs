using System.Collections.Generic;
using Optional;
using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Env = Dictionary<int, string>;
    public class TInput
    {
        public SyntaxNode inputTree { get; }

        public SyntaxNode errNode { get; }

        private Env env { get; }

        public TInput(SyntaxNode inputTree, SyntaxNode errNode, Env env)
        {
            this.inputTree = inputTree;
            this.errNode = errNode;
            this.env = env;
        }

        public string this[int i]
        {
            get => env[i];
        }

        public Option<int> Find(string s)
        {
            foreach (var p in env)
            {
                if (p.Value == s)
                {
                    return Option.Some<int>(p.Key);
                }
            }

            return Option.None<int>();
        }
    }
}