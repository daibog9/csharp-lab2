using System.Text;
using System.Xml;
using System.Xml.Linq;
using Core.Logging;
using Core.Services;

namespace Core.Export;

public interface IFragmentWriter {
    string Extension { get; }
    string MimeType { get; }
    void Write(XDocument fragment, Stream file, string fileName);
}

public sealed class XmlFragmentWriter : IFragmentWriter {
    public string Extension => ".xml";
    public string MimeType => "application/xml";

    public void Write(XDocument fragment, Stream file, string fileName) {
        var settings = new XmlWriterSettings {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        };
        using XmlWriter writer = XmlWriter.Create(file, settings);
        fragment.Save(writer);
    }
}

public sealed class HtmlFragmentWriter(string xslPath, XslHtmlTransformer transformer) : IFragmentWriter {
    public string Extension => ".html";
    public string MimeType => "text/html";

    public void Write(XDocument fragment, Stream file, string fileName) {
        transformer.Transform(fragment, xslPath, file);
        EventLogger.Instance.Log(EventLevel.Transformation,
            $"Сформовано файл {fileName} за XSL-документом {Path.GetFileName(xslPath)}");
    }
}