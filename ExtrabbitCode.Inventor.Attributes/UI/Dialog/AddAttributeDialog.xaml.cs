using ExtrabbitCode.Inventor.Attributes.Models;
using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class AddAttributeDialog
{
    public readonly AddAttributeDialogViewModel ViewModel;

    public AddAttributeDialogResult Result =>
        new(
            ViewModel.AttributeSetName,
            ViewModel.AttributeName,
            ViewModel.SelectedValueType,
            ViewModel.AttributeValue);

    public AddAttributeDialog()
    {
        InitializeComponent();

        ViewModel = new AddAttributeDialogViewModel(Globals.AttributeLibraryService);
        DataContext = ViewModel;

        _ = new WindowInteropHelper(this)
        {
            Owner = new IntPtr(Globals.InvApp.MainFrameHWND)
        };

        RestoreDialogSettings();

        if (Left == 0 && Top == 0)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        
        Closing += OnDialogClosing;

        OkButton.Click += OnOkButtonClick;
        CancelButton.Click += (_, _) => DialogResult = false;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        DragMove();
    }

    private void OnOkButtonClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.ValidateAllInput())
        {
            return;
        }

        DialogResult = true;
    }

    private void OnDialogClosing(object? sender, CancelEventArgs e)
    {
        SaveDialogSettings();
    }

    private void RestoreDialogSettings()
    {
        Left = Properties.Settings.Default.AddAttributeDialogLeft;
        Top = Properties.Settings.Default.AddAttributeDialogTop;
        Width = Properties.Settings.Default.AddAttributeDialogWidth;
        Height = Properties.Settings.Default.AddAttributeDialogHeight;
    }

    private void SaveDialogSettings()
    {
        Properties.Settings.Default.AddAttributeDialogLeft = Left;
        Properties.Settings.Default.AddAttributeDialogTop = Top;
        Properties.Settings.Default.AddAttributeDialogWidth = Width;
        Properties.Settings.Default.AddAttributeDialogHeight = Height;
        Properties.Settings.Default.Save();
    }
}