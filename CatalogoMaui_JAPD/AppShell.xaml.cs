using CatalogoMaui_JAPD.Views;
namespace CatalogoMaui_JAPD
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("producto-detalle", typeof(ProductoDetallePage));
        }
    }
}
