using CommunityToolkit.Mvvm.ComponentModel;

public partial class PollitoDTO : ObservableObject
{
    [ObservableProperty]
    public string Imagen { get; set; }

    [ObservableProperty]
    public string Nombre { get; set; }

    [ObservableProperty]
    public int Puntuacion { get; set; }

    [ObservableProperty]
    public string Ip { get; set; }

    [ObservableProperty]
    public int Posicion { get; set; }

    [ObservableProperty]
    public int Direccion { get; set; }
}