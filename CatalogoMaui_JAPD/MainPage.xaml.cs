using CatalogoMaui.ViewModels;
using CatalogoMaui.Models;

namespace CatalogoMaui_JAPD
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            // Conecta la pantalla con la lista del ViewModel.
            BindingContext = new CatalogoViewModel();
        }


        private bool abriendoDetalle;
        private async void AlSeleccionarProducto(object? sender,
        SelectionChangedEventArgs e)
        {
            if (abriendoDetalle || e.CurrentSelection.Count == 0)
                return;
            if (sender is not CollectionView lista ||
            e.CurrentSelection[0] is not Producto producto)
                return;
            abriendoDetalle = true;
            try
            {
                // Permite tocar la misma tarjeta al regresar.
                lista.SelectedItem = null;
                // Enviamos el objeto completo, no solo su nombre.
                var datos = new ShellNavigationQueryParameters
            {
                { "producto", producto }
            };
                await Shell.Current.GoToAsync("producto-detalle", datos);
            }
            finally
            {
                abriendoDetalle = false;
            }
        }
    }
}




