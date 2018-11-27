using System.IO;
using System.Reflection;

namespace Prem.Transformer.LocLang {
    public static class GrammarText
    {
        public static string Get()
        {
            var assembly = typeof(GrammarText).GetTypeInfo().Assembly;            
            using (var stream = assembly.GetManifestResourceStream("Prem.Transformer.LocLang.LocLang.grammar"))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
