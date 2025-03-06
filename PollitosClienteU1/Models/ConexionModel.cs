using CommunityToolkit.Mvvm.ComponentModel;

namespace PollitosClienteU1.Models
{
    public class ConexionModel: ObservableObject
    {
        [ObservableProperty]
        public string IP { get; set; }

        [ObservableProperty]
        public string Nombre { get; set; }
    }
}