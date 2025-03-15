using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PollitosClienteU1.Models
{
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
        public int Duracion { get; set; } = 5;
        public string Cliente { get; set; }
        public int Direccion { get; set; }
    }
}