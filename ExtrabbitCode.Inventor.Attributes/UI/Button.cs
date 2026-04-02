using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using log4net;
using System.Runtime.CompilerServices;


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
                    DialogHelper.SetDialogTheme(settingsDialog);
                    settingsDialog.ShowDialog();
                    return;

                case "ExtrabbitCode.Inventor.Attributes.AddAttribute":
                    Logger.Debug("Add Attribute Button was pressed.");
                    if (!DialogHelper.CanOpenAddAttributeDialog())
                    {
                        return;
                    }
                    AddAttributeDialog addAttributeDialog = new();
                    DialogHelper.SetDialogTheme(addAttributeDialog);
                    addAttributeDialog.ShowDialog();
                    return;

                case "ExtrabbitCode.Inventor.Attributes.Info":
                    Logger.Info("Info button pressed");
                    InfoDialog infoDialog = new();
                    DialogHelper.SetDialogTheme(infoDialog);
                    infoDialog.ShowDialog();
                    return;
                case "ExtrabbitCode.Inventor.Attributes.Window":
                    Logger.Info("Attribute Window button pressed");

                    DockableWindow? attributeWindow;
                    try
                    {
                        attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows["ExtrabbitCode.Inventor.Attributes.Window"];
                    }
                    catch
                    {
                        attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows.Add("ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes");
                    }

                    attributeWindow.SetMinimumSize(800, 650);
                    attributeWindow.DockingState = DockingStateEnum.kDockLastKnown;
                    attributeWindow.Visible = true;
                    if (!attributeWindow.IsCustomized)
                    {
                        attributeWindow.DockingState = DockingStateEnum.kDockLeft;
                        attributeWindow.Width = 650;
                        attributeWindow.Height = 850;
                    }
                    AttributeDialog attributeDialogContent = new();
                    DialogHelper.SetDialogTheme(attributeDialogContent);

                    DockableWindowChildAdapter.AddWpfWindow(attributeWindow, attributeDialogContent);
                    return;
                default:
                    return;
            }
        }
    }
}