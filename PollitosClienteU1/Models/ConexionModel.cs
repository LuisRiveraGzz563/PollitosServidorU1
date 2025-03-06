using CommunityToolkit.Mvvm.ComponentModel;

namespace PollitosClienteU1.Models
{
    public class ConexionModel: ObservableObject
    {
        [ObservableProperty]
        public string Servidor { get; set; }

        [ObservableProperty]
        public string Usuario { get; set; }
    }
}