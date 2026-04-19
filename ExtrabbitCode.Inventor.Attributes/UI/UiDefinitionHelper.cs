using System;
using System.Linq;
using System.Windows.Forms;

namespace ExtrabbitCode.Inventor.Attributes.UI;

public static class UiDefinitionHelper
{
    public static ButtonDefinition CreateButton(string displayText, string internalName, string iconPath, string theme)
    {
        UiButton myButton = new()
        {
            Bd = CreateButtonDefinition(displayText, internalName, "", iconPath, theme)
        };
        return myButton.Bd;
    }

    public static ButtonDefinition CreateButtonDefinition(string displayName, string internalName,
        string toolTip, string iconFolder, string theme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(internalName);

        ButtonDefinition? controlDefs = Globals.InvApp.CommandManager.ControlDefinitions
            .Cast<object>()
            .OfType<ButtonDefinition>()
            .FirstOrDefault(b =>
                string.Equals(b.InternalName, internalName, StringComparison.OrdinalIgnoreCase));

        if (controlDefs is not null)
        {
            throw new InvalidOperationException(
                $"A command already exists with the internal name '{internalName}'. " +
                "Each add-in must have a unique internal name.");
        }

        iconFolder = GetIconFolder(iconFolder);

        (IPictureDisp? iPicDisp16X16, IPictureDisp? iPicDisp32X32) = GetButtonIcons(iconFolder, theme);

        try
        {
            CommandManager cmdMgr = Globals.InvApp.CommandManager;
            ButtonDefinition? buttonDef = cmdMgr.ControlDefinitions.AddButtonDefinition(
                displayName,
                internalName,
                CommandTypesEnum.kShapeEditCmdType,
                Globals.AddInClientId,
                string.Empty,
                toolTip,
                iPicDisp16X16,
                iPicDisp32X32
            );

            return buttonDef;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create button definition.", ex);
        }
    }

    public static string GetIconFolder(string iconFolder)
    {
        if (string.IsNullOrEmpty(iconFolder))
        {
            return iconFolder;
        }

        if (!System.IO.Directory.Exists(iconFolder))
        {
            string? dllPath =
                System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            iconFolder = System.IO.Path.Combine(dllPath ?? throw new InvalidOperationException(),
                iconFolder);
        }

        return iconFolder;
    }

    public static (IPictureDisp? iPicDisp16X16, IPictureDisp? iPicDisp32X32) GetButtonIcons(string iconFolder, string theme)
    {
        IPictureDisp? iPicDisp16X16 = null;
        IPictureDisp? iPicDisp32X32 = null;
        if (string.IsNullOrEmpty(iconFolder) || !System.IO.Directory.Exists(iconFolder))
        {
            return (null, null);
        }

        string filename16X16 = System.IO.Path.Combine(iconFolder, $"16x16{theme}.png");
        string filename32X32 = System.IO.Path.Combine(iconFolder, $"32x32{theme}.png");

        if (System.IO.File.Exists(filename16X16))
        {
            try
            {
                using System.Drawing.Bitmap image16X16 = new(filename16X16);
                iPicDisp16X16 = ConvertImage.ConvertImageToIPictureDisp(image16X16);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $@"Unable to load the 16x16.png image from ""{iconFolder}"". No small icon will be used.",
                    ex);
            }
        }
        else
        {
            MessageBox.Show(
                @"The icon for the small button does not exist: """ + filename16X16 + @"""." +
                System.Environment.NewLine + @"No small icon will be used.", @"Error Loading Icon");
        }

        if (System.IO.File.Exists(filename32X32))
        {
            try
            {
                using System.Drawing.Bitmap image32X32 = new(filename32X32);
                iPicDisp32X32 = ConvertImage.ConvertImageToIPictureDisp(image32X32);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $@"Unable to load the 32x32.png image from ""{iconFolder}"". No large icon will be used.",
                    ex);
            }
        }
        else
        {
            MessageBox.Show(
                @"The icon for the large button does not exist: """ + filename32X32 + @"""." +
                System.Environment.NewLine + @"No large icon will be used.", @"Error Loading Icon");
        }

        return (iPicDisp16X16, iPicDisp32X32);
    }

    public static RibbonTab SetupTab(string displayName, string internalName, Ribbon? invRibbon)
    {
        ArgumentNullException.ThrowIfNull(invRibbon);

        RibbonTab? ribbonTab = invRibbon.RibbonTabs
            .Cast<RibbonTab>()
            .FirstOrDefault(t => string.Equals(t.InternalName, internalName, StringComparison.OrdinalIgnoreCase));

        if (ribbonTab is not null)
        {
            return ribbonTab;
        }

        try
        {
            ribbonTab = invRibbon.RibbonTabs.Add(displayName, internalName, Globals.AddInClientId);
            return ribbonTab;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create ribbon tab '{internalName}' in ribbon '{invRibbon.InternalName}'.", ex);
        }
    }

    public static RibbonPanel SetupPanel(string displayName, string internalName, RibbonTab? ribbonTab)
    {
        ArgumentNullException.ThrowIfNull(ribbonTab);

        RibbonPanel? ribbonPanel = ribbonTab.RibbonPanels
            .Cast<RibbonPanel>()
            .FirstOrDefault(p =>
                string.Equals(p.InternalName, internalName, StringComparison.OrdinalIgnoreCase));

        if (ribbonPanel is not null)
        {
            return ribbonPanel;
        }

        try
        {
            ribbonPanel = ribbonTab.RibbonPanels.Add(displayName, internalName, Globals.AddInClientId);
            return ribbonPanel;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create ribbon panel '{internalName}' in tab '{ribbonTab.InternalName}'.",
                ex);
        }
    }
}

///<summary>
/// Class used to convert bitmaps and icons between their .Net native types
/// and an IPictureDisp object which is what the Inventor API requires.
/// </summary>
public class ConvertImage() : AxHost("59EE46BA-677D-4d20-BF10-8D8067CB8B32")
{
    public static IPictureDisp ConvertImageToIPictureDisp(System.Drawing.Image image)
    {
        try
        {
            return (IPictureDisp)GetIPictureFromPicture(image);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to convert image to IPictureDisp. The input image might be invalid or unsupported.",
                ex);
        }
    }
}