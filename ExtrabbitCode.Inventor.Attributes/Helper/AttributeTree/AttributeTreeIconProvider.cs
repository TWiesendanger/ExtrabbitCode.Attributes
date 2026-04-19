using ExtrabbitCode.Inventor.Attributes.Models;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExtrabbitCode.Inventor.Attributes.Helper.AttributeTree;

public static class AttributeTreeIconProvider
{
    public static ImageSource? GetDocumentIcon(Document? document)
    {
        if (document != null)
        {
            return document.DocumentType switch
            {
                DocumentTypeEnum.kPartDocumentObject =>
                    CreateImageSource("Resources/part.png"),

                DocumentTypeEnum.kAssemblyDocumentObject =>
                    CreateImageSource("Resources/assembly.png"),

                DocumentTypeEnum.kDrawingDocumentObject =>
                    CreateImageSource("Resources/drawing.png"),

                DocumentTypeEnum.kPresentationDocumentObject =>
                    CreateImageSource("Resources/presentation.png"),

                _ => null
            };
        }

        return null;
    }

    public static ImageSource? GetIcon(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.AttributeSet => GetAttributeSetIcon(),
            NodeType.Attribute => GetAttributeIcon(),
            NodeType.Owner => GetOwnerIcon(),
            NodeType.OrphanAttributeSet => GetAttributeSetIcon(),
            NodeType.OrphanRoot => GetOwnerIcon(),
            _ => null
        };
    }

    private static BitmapImage GetOwnerIcon()
    {
        return CreateImageSource("Resources/other.png");
    }

    private static BitmapImage GetAttributeSetIcon()
    {
        return CreateImageSource("Resources/attributeSet.png");
    }

    private static BitmapImage GetAttributeIcon()
    {
        return CreateImageSource("Resources/attribute.png");
    }

    private static BitmapImage CreateImageSource(string relativePath)
    {
        string uri =
            $"pack://application:,,,/ExtrabbitCode.Inventor.Attributes;component/{relativePath}";

        return new BitmapImage(new Uri(uri, UriKind.Absolute));
    }
}