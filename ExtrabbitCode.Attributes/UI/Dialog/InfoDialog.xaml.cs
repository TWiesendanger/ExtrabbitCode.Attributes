using ExtrabbitCode.Attributes.UI.ViewModels;
using ExtrabbitCode.Inventor.ModernUi;
using System;
using System.Windows;
using System.Windows.Interop;

namespace ExtrabbitCode.Attributes.UI.Dialog;

public partial class InfoDialog
{
    public InfoDialog()
    {
        InitializeComponent();
        ModernUi.Apply(this, Globals.CurrentTheme, font: Globals.CurrentFont);
        DataContext = new InfoDialogViewModel();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        IntPtr ownerHandle = new(Globals.InvApp.MainFrameHWND);
        new WindowInteropHelper(this).Owner = ownerHandle;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
