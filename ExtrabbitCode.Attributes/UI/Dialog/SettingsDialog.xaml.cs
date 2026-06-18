using ExtrabbitCode.Attributes.UI.ViewModels;
using ExtrabbitCode.Inventor.ModernUi;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace ExtrabbitCode.Attributes.UI.Dialog;

public partial class SettingsDialog
{
    private readonly SettingsDialogViewModel _viewModel;

    public SettingsDialog()
    {
        InitializeComponent();
        ModernUi.Apply(this, Globals.CurrentTheme, font: Globals.CurrentFont);
        _viewModel = new SettingsDialogViewModel(Globals.SettingsService, Globals.AttributeLibraryService);
        DataContext = _viewModel;

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        IntPtr ownerHandle = new(Globals.InvApp.MainFrameHWND);
        new WindowInteropHelper(this).Owner = ownerHandle;
    }


    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.Save();
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TelemetryLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
