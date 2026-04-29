using ExtrabbitCode.Inventor.Attributes.Addin;
using ExtrabbitCode.Inventor.Attributes.Helper;
using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI.Dialog;
using log4net;
using System.Runtime.CompilerServices;

namespace ExtrabbitCode.Inventor.Attributes.UI;

public class UiButton
{
    private const string DocumentationUrl = "https://twiesendanger.github.io/ExtrabbitCode.Inventor.Attributes/";
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
                Globals.TelemetryService.TrackEvent("panel_button_settings_clicked");
                SettingsDialog settingsDialog = new();
                DialogHelper.SetDialogTheme(settingsDialog);
                settingsDialog.ShowDialog();
                return;
            case "ExtrabbitCode.Inventor.Attributes.AddAttribute":
                Logger.Debug("Add Attribute Button was pressed.");
                Globals.TelemetryService.TrackEvent("panel_button_add_attribute_clicked");
                AddAttributeWorkflowResult workflowResult = Globals.AddAttributeWorkflowService.Execute();

                if (!workflowResult.IsSuccess &&
                    !string.IsNullOrWhiteSpace(workflowResult.ErrorMessage))
                {
                    DialogHelper.ShowInfoMessage(
                        "Add Attribute",
                        workflowResult.ErrorMessage);
                }

                return;
            case "ExtrabbitCode.Inventor.Attributes.Info":
                Logger.Info("Info button pressed");
                Globals.TelemetryService.TrackEvent("panel_button_info_clicked");
                InfoDialog infoDialog = new();
                DialogHelper.SetDialogTheme(infoDialog);
                infoDialog.ShowDialog();
                return;
            case "ExtrabbitCode.Inventor.Attributes.Help":
                Logger.Info("Help button pressed");
                Globals.TelemetryService.TrackEvent("panel_button_help_clicked");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = DocumentationUrl,
                    UseShellExecute = true
                });
                return;
            case "ExtrabbitCode.Inventor.Attributes.OpenAttributeWindow":
                Logger.Info("Attribute Window button pressed");
                Globals.TelemetryService.TrackEvent("panel_button_attribute_window_clicked");

                DockableWindow? attributeWindow;
                try
                {
                    attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows["ExtrabbitCode.Inventor.Attributes.Window"];
                }
                catch
                {
                    attributeWindow = Globals.InvApp.UserInterfaceManager.DockableWindows.Add("ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes.Window", "ExtrabbitCode.Inventor.Attributes");
                }

                attributeWindow.SetMinimumSize(800, 800);
                attributeWindow.DockingState = DockingStateEnum.kDockLastKnown;
                attributeWindow.Visible = true;
                if (!attributeWindow.IsCustomized)
                {
                    attributeWindow.DockingState = DockingStateEnum.kDockLeft;
                    attributeWindow.Width = 800;
                    attributeWindow.Height = 850;
                }
                AttributeDialog attributeDialogContent = new();

                DockableWindowChildAdapter.AddWpfWindow(attributeWindow, attributeDialogContent);
                DialogHelper.SetDialogTheme(attributeDialogContent);
                return;
            default:
                return;
        }
    }
}