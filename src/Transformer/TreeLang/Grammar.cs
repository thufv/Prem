using System.IO;
using System.Reflection;

namespace Prem.Transformer.TreeLang {
    public static class GrammarText
    {
        public static string Get()
        {
            var assembly = typeof(GrammarText).GetTypeInfo().Assembly;            
            using (var stream = assembly.GetManifestResourceStream("Prem.Transformer.TreeLang.TreeLang.grammar"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
