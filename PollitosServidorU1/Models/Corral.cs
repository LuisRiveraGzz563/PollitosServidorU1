using CommunityToolkit.Mvvm.ComponentModel;
using PollitosServidorU1.Services;
using System;
using System.Collections.ObjectModel;

namespace PollitosServidorU1.Models
{
    public partial class Corral : ObservableObject
    {
        
        [ObservableProperty]
        public ObservableCollection<PollitoDTO> Pollos { get; set; } = new ObservableCollection<PollitoDTO>();
        [ObservableProperty]
        public PollitoDTO Pollito { get; set; } = new PollitoDTO();
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