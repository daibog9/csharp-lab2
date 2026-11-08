using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Core.Logging;
using Core.Search;

namespace Core.Services;

public sealed class XslHtmlTransformer {
    public void TransformFile(string xmlPath, string xslPath, string htmlPath) {
        using XmlReader input = XmlReader.Create(xmlPath, XmlValues.ReaderSettings());
        TransformToFile(input, xslPath, htmlPath);
    }

    public void Transform(XDocument document, string xslPath, string htmlPath) {
        using XmlReader input = document.CreateReader();
        TransformToFile(input, xslPath, htmlPath);
    }

    public void Transform(XDocument document, string xslPath, Stream output) {
        using XmlReader input = document.CreateReader();
        LoadStylesheet(xslPath).Transform(input, arguments: null, output);
    }

    public void ValidateStylesheet(string xslPath) => LoadStylesheet(xslPath);

    private static void TransformToFile(XmlReader input, string xslPath, string htmlPath) {
        XslCompiledTransform xslt = LoadStylesheet(xslPath);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        using (FileStream output = File.Create(htmlPath)) {
            xslt.Transform(input, arguments: null, output);
        }
        EventLogger.Instance.Log(EventLevel.Transformation, $"Збережено у файл {Path.GetFileName(htmlPath)}");
    }

    private static XslCompiledTransform LoadStylesheet(string xslPath) {
        var xslt = new XslCompiledTransform();
        try {
            using XmlReader stylesheet = XmlReader.Create(xslPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            xslt.Load(stylesheet);
        } catch (Exception ex) when (ex is XmlException or XsltException) {
            throw new InvalidDataException($"Не вдалося завантажити XSL-документ. {ex.Message}", ex);
        }
        return xslt;
    }
}