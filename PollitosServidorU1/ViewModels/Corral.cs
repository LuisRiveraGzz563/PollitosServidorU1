using PollitosServidorU1.Models;
using PollitosServidorU1.Services;
using System;
using System.Collections.Generic;

namespace PollitosServidorU1.ViewModels
{
    public class Corral
    {
        public List<PollitoDTO> Pollos { get; set; } = new List<PollitoDTO>();
        public PollitoDTO Pollo { get; set; }
        public TcpService Servidor { get; set; } = new TcpService();

        private readonly int NumMaiz = 5;
        private readonly Random r = new Random();
        public Corral()
        {
            for (int i = 0; i < 25; i++)
            {
                Pollos.Add(null);
            }
            //Esto muestra un usuario de muestra
            Pollos[1] = new PollitoDTO
            {
                Imagen = "🐥",
                Nombre = "Pollito 1",
                Puntuacion = 0,
                IP = "192.168.1.1"
            };
            GenerarMaiz();

            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
        }

        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            Pollos[dto.Posicion] = dto;
        }

        //Metodo para generar maiz de manera aleatoria en el corral
        void GenerarMaiz()
        {
            for (int i = 0; i < NumMaiz; i++)
            {
                int posicion = r.Next(0, 25);
                if (Pollos[posicion] == null)
                {
                    Pollos[posicion] = new PollitoDTO
                    {
                        Imagen = "🌽",
                        Puntuacion = -10,
                    };
                    generated++;
                }
            }
        }

        // Método para mover el pollito
        public void MoverPollito(int direccion)
        {
            if (Pollo == null) return;

            int nuevaPosicion = Pollo.Posicion;

            switch (direccion)
            {
                //Arriba
                case 1:
                    if (nuevaPosicion >= Renglones) // Evitar movimiento fuera de los límites
                        nuevaPosicion -= Renglones; // Subir
                    break;
                //Abajo
                case 2:
                    if (nuevaPosicion < (TamañoCorral - Renglones)) // Evitar movimiento fuera de los límites
                        nuevaPosicion += Renglones; // Bajar
                    break;
                //izquierda
                case 3:
                    if (nuevaPosicion % Columnas != 0) // Evitar movimiento fuera de los límites
                        nuevaPosicion -= 1; // Mover a la izquierda
                    break;
                //derecha
                case 4:
                    if (nuevaPosicion % Columnas != (Columnas - 1)) // Evitar movimiento fuera de los límites
                        nuevaPosicion += 1; // Mover a la derecha
                    break;
                }
            }
        }
    }
}