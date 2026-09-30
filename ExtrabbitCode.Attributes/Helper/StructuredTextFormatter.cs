using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace ExtrabbitCode.Attributes.Helper;

public enum StructuredTextKind
{
    None,
    Json,
    Xml
}

/// <summary>
/// Detects JSON and XML inside string attribute values and converts them
/// between an indented (readable) and a compact (single line) representation.
/// </summary>
internal static class StructuredTextFormatter
{
    private static readonly JsonWriterOptions IndentedJsonOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonWriterOptions CompactJsonOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Returns the kind of structured text the value looks like, based on its first character.
    /// Does not check whether the value is actually well-formed.
    /// </summary>
    public static StructuredTextKind GetCandidateKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StructuredTextKind.None;
        }

        char first = value.TrimStart()[0];

        return first switch
        {
            '{' or '[' => StructuredTextKind.Json,
            '<' => StructuredTextKind.Xml,
            _ => StructuredTextKind.None
        };
    }

    /// <summary>
    /// Formats JSON or XML text. Returns false if the value is neither, or if it looks like
    /// JSON/XML but is not well-formed (in which case <paramref name="error"/> describes why).
    /// </summary>
    public static bool TryFormat(
        string? value,
        bool indented,
        out string formatted,
        out StructuredTextKind kind,
        out string? error)
    {
        formatted = value ?? string.Empty;
        error = null;
        kind = GetCandidateKind(value);

        if (kind == StructuredTextKind.None)
        {
            return false;
        }

        try
        {
            formatted = kind == StructuredTextKind.Json
                ? FormatJson(value!, indented)
                : FormatXml(value!, indented);
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
        }
        catch (XmlException ex)
        {
            error = ex.Message;
        }

        formatted = value!;
        return false;
    }

    private static string FormatJson(string value, bool indented)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, indented ? IndentedJsonOptions : CompactJsonOptions))
        {
            document.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string FormatXml(string value, bool indented)
    {
        XmlReaderSettings readerSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreWhitespace = indented
        };

        XDocument document;
        using (StringReader stringReader = new(value.Trim()))
        using (XmlReader reader = XmlReader.Create(stringReader, readerSettings))
        {
            document = XDocument.Load(reader, indented ? LoadOptions.None : LoadOptions.PreserveWhitespace);
        }

        if (!indented)
        {
            // Drop whitespace-only text nodes at document level and between elements, keep text content untouched.
            foreach (XText whitespace in document.DescendantNodes()
                         .OfType<XText>()
                         .Where(t => t is not XCData &&
                                     string.IsNullOrWhiteSpace(t.Value) &&
                                     (t.Parent is null || t.Parent.Elements().Any()))
                         .ToList())
            {
                whitespace.Remove();
            }
        }

        XmlWriterSettings writerSettings = new()
        {
            Indent = indented,
            IndentChars = "  ",
            OmitXmlDeclaration = document.Declaration is null,
            NewLineHandling = NewLineHandling.None
        };

        StringBuilder builder = new();
        using (XmlWriter writer = XmlWriter.Create(builder, writerSettings))
        {
            document.Save(writer);
        }

        // XmlWriter writing into a StringBuilder always declares utf-16; restore the original declaration.
        if (document.Declaration is not null)
        {
            int declarationEnd = builder.ToString().IndexOf("?>", StringComparison.Ordinal);
            if (declarationEnd >= 0)
            {
                builder.Remove(0, declarationEnd + 2);
                builder.Insert(0, document.Declaration.ToString());
            }
        }

        return builder.ToString();
    }
}
