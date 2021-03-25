public class Program
{
    private Dictionary<string, int> ExtractNodeInfo(string fileContent)
    {
        XmlDocument xmlDocument;
        xmlDocument = new XmlDocument();
        xmlDocument.Load(fileContent);
        var ediNodes = xmlDocument.DocumentElement.SelectNodes("/EDI");
        Dictionary<string, int> defaultValue = new Dictionary<string, int>();
        foreach (XmlNode nodes in ediNodes)
        {
            FileManager.nodeRecurse(nodes, defaultValue);
        }

        foreach (var entry in defaultValue)
        {
            Console.WriteLine(entry.ToString());
        }
    }

}