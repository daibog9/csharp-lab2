using System.Xml;

namespace Core.Search;

public interface ISaxContentHandler {
    void StartDocument();
    void StartElement(string name, IReadOnlyList<KeyValuePair<string, string>> attributes);
    void Characters(string text);
    void EndElement(string name);
    void EndDocument();
}

public static class SaxParser {
    public static void Parse(string xmlPath, ISaxContentHandler handler) {
        using XmlReader reader = XmlReader.Create(xmlPath, XmlValues.ReaderSettings());

        handler.StartDocument();
        while (reader.Read()) {
            switch (reader.NodeType) {
                case XmlNodeType.Element:
                    string name = reader.Name;
                    bool isEmpty = reader.IsEmptyElement;
                    handler.StartElement(name, ReadAttributes(reader));
                    if (isEmpty) handler.EndElement(name);
                    break;

                case XmlNodeType.EndElement:
                    handler.EndElement(reader.Name);
                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    handler.Characters(reader.Value);
                    break;
            }
        }
        handler.EndDocument();
    }

    private static List<KeyValuePair<string, string>> ReadAttributes(XmlReader reader) {
        var attributes = new List<KeyValuePair<string, string>>(reader.AttributeCount);
        if (reader.MoveToFirstAttribute()) {
            do {
                attributes.Add(new(reader.Name, reader.Value));
            } while (reader.MoveToNextAttribute());
            reader.MoveToElement();
        }
        return attributes;
    }
}