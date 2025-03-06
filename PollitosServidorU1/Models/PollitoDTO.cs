using CommunityToolkit.Mvvm.ComponentModel;

public partial class PollitoDTO : ObservableObject
{
    [ObservableProperty]
    public string Nombre { get; set; }
    [ObservableProperty]
    public int Puntuacion { get; set; }
    [ObservableProperty]
    public string Imagen { get; set; }
    [ObservableProperty]
    public int Posicion { get; set; }

    public string Cliente { get; set; }
    public int Direccion { get; set; }
}