using System;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis.Utils;

using Prem.Util;

namespace Prem.Transformer.TreeLang
{
    using Env = Dictionary<EnvKey, string>;

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

        public string this[EnvKey key]
        {
            get => env[key];
        }

        public Optional<EnvKey> Find(string s)
        {
            foreach (var p in env)
            {
                if (p.Value == s)
                {
                    return p.Key.Some();
                }
            }

            return Optional<EnvKey>.Nothing;
        }

        public override string ToString()
        {
            return $"err:{errNode}";
        }
    }
}