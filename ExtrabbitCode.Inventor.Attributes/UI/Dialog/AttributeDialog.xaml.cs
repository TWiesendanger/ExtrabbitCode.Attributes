using ExtrabbitCode.Inventor.Attributes.UI.ViewModels;
using Wpf.Ui.Appearance;

namespace ExtrabbitCode.Inventor.Attributes.UI.Dialog;

public partial class AttributeDialog
{
    public AttributeDialog()
    {
        InitializeComponent();
        DataContext = new AttributeWindowViewModel();

        ApplicationThemeManager.Apply(this);
    }
}