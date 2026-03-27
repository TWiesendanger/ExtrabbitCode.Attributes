using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class SettingsDialog
{
    private readonly SettingsDialogViewModel _viewModel;

    public SettingsDialog()
    {
        InitializeComponent();
        _viewModel = new SettingsDialogViewModel(Globals.SettingsService, Globals.AttributeLibraryService);
        DataContext = _viewModel;

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        IntPtr ownerHandle = new(Globals.InvApp.MainFrameHWND);
        new WindowInteropHelper(this).Owner = ownerHandle;


        //ApplicationThemeManager.Apply(this);

        WindowStartupLocation = WindowStartupLocation.CenterOwner;
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

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        DragMove();
    }
}
