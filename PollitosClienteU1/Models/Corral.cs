using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PollitosClienteU1.Models
{
    public partial class Corral: ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<PollitoDTO> Pollos { get; set; } = new ObservableCollection<PollitoDTO>();
        public Corral(int TamañoCorral)
        {
            // Inicializar la colección con valores nulos
            for (int i = 0; i < TamañoCorral; i++)
            {
                Pollos.Add(null);
            }
        }

    }
}
