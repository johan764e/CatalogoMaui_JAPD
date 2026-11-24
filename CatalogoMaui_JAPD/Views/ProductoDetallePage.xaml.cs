using System.Globalization;
using CatalogoMaui.Models;
using CatalogoMaui.Services;
using System.Collections.Generic;
using CatalogoMaui.Models;
namespace CatalogoMaui_JAPD.Views;

public partial class ProductoDetallePage : ContentPage, IQueryAttributable
{
    private int id;
    private string fotoElegida = "";   // imagen incluida o foto propia ya guardada
    private string fotoOriginal = "";  // la que tenía el producto al abrir
    private string? temporal;          // copia temporal de la foto nueva

    public ProductoDetallePage()
    {
        InitializeComponent();
        Title = "Agregar producto";
        Foto.ItemsSource = new List<string> { "mochila.jpg", "libreta.jpg", "termo.jpg" };
        Foto.SelectedIndex = 0;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("producto", out var dato) || dato is not Producto p)
            return;

        id = p.Id;
        Title = "Editar producto";
        Nombre.Text = p.Nombre;
        Descripcion.Text = p.Descripcion;
        Detalle.Text = p.Detalle;
        Precio.Text = p.Precio.ToString("0.00", CultureInfo.CurrentCulture);
        Foto.SelectedItem = p.Foto;
        fotoOriginal = fotoElegida = p.Foto;
        VistaFoto.Source = FotosProducto.Resolver(p.Foto);
        Eliminar.IsVisible = true;
    }

    // Elegir una imagen incluida descarta la foto nueva pendiente.
    private void AlCambiarFoto(object? sender, EventArgs e)
    {
        if (Foto.SelectedItem is not string archivo) return;
        FotosProducto.BorrarTemporal(temporal);
        temporal = null;
        fotoElegida = archivo;
        VistaFoto.Source = archivo;
    }

    private async void AlElegirFoto(object? sender, EventArgs e)
    {
        try
        {
            string? nueva = await FotosProducto.ElegirAsync();
            if (nueva is null) return;   // cancelado: no cambia nada

            FotosProducto.BorrarTemporal(temporal);
            Foto.SelectedIndex = -1;
            temporal = nueva;
            VistaFoto.Source = nueva;
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se pudo elegir la foto", ex.Message, "Aceptar");
        }
    }

    private async void AlGuardar(object? sender, EventArgs e)
    {
        if (!Guardar.IsEnabled) return;

        var textoPrecio = Precio.Text?.Trim().Replace(',', '.');
        if (string.IsNullOrWhiteSpace(Nombre.Text) ||
            string.IsNullOrWhiteSpace(Descripcion.Text) ||
            string.IsNullOrWhiteSpace(Detalle.Text) ||
            !decimal.TryParse(textoPrecio, NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var importe) ||
            importe < 0 || importe > 99999999.99m ||
            decimal.Round(importe, 2) != importe ||
            (temporal is null && string.IsNullOrEmpty(fotoElegida)))
        {
            await DisplayAlert("Revisa los datos",
                "Completa los campos y escribe un precio válido de hasta dos decimales.", "Aceptar");
            return;
        }

        string? copiaNueva = null;
        await EjecutarAsync(async () =>
        {
            // 1) copiar la foto nueva
            if (temporal is not null)
                copiaNueva = await FotosProducto.GuardarAsync(temporal);

            var producto = new Producto
            {
                Id = id, Nombre = Nombre.Text.Trim(), Descripcion = Descripcion.Text.Trim(),
                Detalle = Detalle.Text.Trim(), Precio = importe,
                Foto = copiaNueva ?? fotoElegida
            };

            try
            {
                // 2) guardar en SQLite
                if (id == 0)
                    await ProductosSqlite.Instancia.Insertar(producto);
                else if (!await ProductosSqlite.Instancia.Actualizar(producto))
                    throw new InvalidOperationException("El producto ya no existe.");
            }
            catch
            {
                // Si falla SQLite, se borra la copia nueva y se conserva la anterior.
                FotosProducto.Eliminar(copiaNueva);
                throw;
            }

            // 3) solo si se guardó, retirar la foto propia anterior
            if (producto.Foto != fotoOriginal)
                FotosProducto.Eliminar(fotoOriginal);

            FotosProducto.BorrarTemporal(temporal);
            return true;
        });
    }

    private async void AlEliminar(object? sender, EventArgs e)
    {
        if (!Eliminar.IsEnabled || id == 0) return;
        await EjecutarAsync(async () =>
        {
            if (!await DisplayAlert("Eliminar producto", "¿Deseas eliminar este producto?",
                "Eliminar", "Cancelar"))
                return false;
            if (!await ProductosSqlite.Instancia.Eliminar(id))
                throw new InvalidOperationException("El producto ya no existe.");
            FotosProducto.Eliminar(fotoOriginal);
            return true;
        });
    }

    private async Task EjecutarAsync(Func<Task<bool>> operacion)
    {
        Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = false;
        try
        {
            if (await operacion()) await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("No se completó la operación", ex.Message, "Aceptar");
        }
        finally
        {
            Guardar.IsEnabled = Eliminar.IsEnabled = ElegirFoto.IsEnabled = true;
        }
    }
}
