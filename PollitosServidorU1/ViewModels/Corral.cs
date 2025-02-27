using PollitosServidorU1.Models;
using System.Collections.Generic;

namespace PollitosServidorU1.ViewModels
{
    public class Corral
    {
        public List<PollitoDTO> Pollos { get; set; } = new List<PollitoDTO>();
        public PollitoDTO Pollo { get; set; }
        public Corral()
        {
            for (int i = 0; i < 25; i++)
            {
                Pollos.Add(null);
            }

            Pollos[1]= new PollitoDTO
            {
                Imagen = "🐥",
                Nombre = "Pollito 1",
                Puntuacion = 100,
                Posicion = 1,
                IP = "192.168.1.1"
            };

        }
    }
}
