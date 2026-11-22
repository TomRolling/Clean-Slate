using System.Windows.Controls;

namespace CleanSlate.Views;

// Le « code-behind » des trois écrans. Il ne fait que charger le XAML :
// toute la logique est dans CatalogueViewModel (pattern MVVM).

public partial class CatalogueView : UserControl
{
    public CatalogueView() => InitializeComponent();
}

public partial class ReviewView : UserControl
{
    public ReviewView() => InitializeComponent();
}

public partial class InstallView : UserControl
{
    public InstallView() => InitializeComponent();
}
