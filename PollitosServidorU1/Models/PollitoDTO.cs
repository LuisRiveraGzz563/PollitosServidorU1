using CommunityToolkit.Mvvm.ComponentModel;

public partial class PollitoDTO : ObservableObject
{
    [ObservableProperty]
    public string Nombre;
    [ObservableProperty]
    public int Puntuacion;   
    [ObservableProperty]
    public string Imagen;
    [ObservableProperty]
    public int Posicion;

    public string Cliente;
    public int Direccion;
}