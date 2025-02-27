using CommunityToolkit.Mvvm.ComponentModel;

namespace PollitosServidorU1.Models
{
    public partial class PollitoDTO: ObservableObject
    {
        [ObservableProperty]
        public string Imagen { get; set; }
        [ObservableProperty]
        public string Nombre { get; set; }
        [ObservableProperty]
        public int Puntuacion { get; set; }
        [ObservableProperty]
        public int Posicion { get; set; }
        [ObservableProperty]
        public string IP { get; set; }
    }
}
