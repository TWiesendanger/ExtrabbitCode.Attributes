using ExtrabbitCode.Attributes.Helper;
using ExtrabbitCode.Attributes.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ExtrabbitCode.Attributes.UI.Dialog;

public partial class InfoDialog
{
    public InfoDialog()
    {
        InitializeComponent();
        DialogHelper.SetDialogTheme(this);
        DataContext = new InfoDialogViewModel();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        IntPtr ownerHandle = new(Globals.InvApp.MainFrameHWND);
        new WindowInteropHelper(this).Owner = ownerHandle;


        WindowStartupLocation = WindowStartupLocation.CenterOwner;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        DragMove();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}