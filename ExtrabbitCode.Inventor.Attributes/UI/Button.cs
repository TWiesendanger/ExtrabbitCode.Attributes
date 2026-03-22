using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using log4net;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using Wpf.Ui.Appearance;


namespace ExtrabbitCode.Inventor.Attributes.UI
{
    public class UiButton
    {
        private ButtonDefinition? _bd;
        private static readonly ILog Logger = LogManagerAddin.GetLogger(typeof(UiButton));

        public ButtonDefinition? Bd {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get => _bd;

            [MethodImpl(MethodImplOptions.Synchronized)]
            set {
                if (_bd != null)
                {
                    _bd.OnExecute -= ButtonOnExecute;
                }

                _bd = value;
                if (_bd != null)
                {
                    _bd.OnExecute += ButtonOnExecute;
                }
            }
        }

        private void ButtonOnExecute(NameValueMap context)
        {
            if (Bd is null)
            {
                Logger.Error("ButtonOnExecute invoked, but Bd is null.");
                return;
            }

            switch (Bd.InternalName)
            {
                case "ExtrabbitCode.Inventor.Attributes.SettingsButton":
                    Logger.Debug("Settings Button was pressed.");
                    SettingsDialog settingsDialog = new();
                    SetDialogTheme(settingsDialog);
                    settingsDialog.ShowDialog();
                    return;
                case "ExtrabbitCode.Inventor.Attributes.Info":
                    Logger.Info("Info button pressed");
                    InfoDialog infoDialog = new();
                    SetDialogTheme(infoDialog);
                    infoDialog.ShowDialog();
                    return;
                case "ExtrabbitCode.Inventor.Attributes.Window":
                    Logger.Info("Attribute Window button pressed");

                    DockableWindow? attributeWindow;
                    try
                    {
                        attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows["ExtrabbitCode.Inventor.Attributes.Window"];
                    }
#pragma warning disable CA1031
                    catch
#pragma warning restore CA1031
                    {
                        attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows.Add("ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes");
                    }

                    attributeWindow.Visible = true;
                    AttributeDialog attributeDialogContent = new();
                    SetDialogTheme(attributeDialogContent);

                    DockableWindowChildAdapter.AddWpfWindow(attributeWindow, attributeDialogContent);
                    return;
                default:
                    return;
            }
        }

        private static void SetDialogTheme(Window dialog)
        {
            ApplicationTheme theme = Globals.ActiveTheme.Name == InventorThemeConstants.LightTheme
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            ApplicationThemeManager.Apply(dialog);
            ApplicationThemeManager.Apply(theme);
        }
    }
}