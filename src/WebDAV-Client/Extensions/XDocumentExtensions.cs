using System.Xml.Linq;

// ReSharper disable once CheckNamespace
namespace WebDav;

internal static class XDocumentExtensions
{
    public static XDocument? TryParse(string text)
    {
        try
        {
            return XDocument.Parse(text);
        }
        catch
        {
            return null;
        }
    }
}