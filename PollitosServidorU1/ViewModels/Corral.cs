using CommunityToolkit.Mvvm.ComponentModel;
using PollitosServidorU1.Services;
using System;
using System.Collections.ObjectModel;

namespace PollitosServidorU1.ViewModels
{
    public partial class Corral : ObservableObject
    {
        private readonly string[] Images = new string[2] { "🐥", "🌽" };

        [ObservableProperty]
        private readonly ObservableCollection<PollitoDTO> Pollos = new ObservableCollection<PollitoDTO>();

        [ObservableProperty]
        private readonly PollitoDTO Pollo;

        public TcpService Servidor { get; set; } = new TcpService();

        private readonly int NumMaiz = 5;
        public int Renglones { get; set; } = 10;
        public int Columnas { get; set; } = 10;
        private readonly int TamañoCorral;
        private readonly Random r = new Random();

        public Corral()
        {
            TamañoCorral = Renglones * Columnas;

            // Inicializar la colección con valores nulos
            for (int i = 0; i < TamañoCorral; i++)
            {
                Pollos.Add(null);
            }

            // Agregar un pollito de prueba
            Pollo = new PollitoDTO
            {
                Imagen = Images[0],
                Nombre = "Pollito 1",
                Puntuacion = 0,
                Ip = "192.168.1.1",
                Posicion = 1
            };

            Pollos[1] = Pollo;

            GenerarMaiz();

            // Suscribir al evento de recepción de pollitos
            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
        }

        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            if (EsMovimientoValido(dto.Posicion, dto.Direccion))
            {
                MoverPollito(dto.Direccion);
            }
        }

        private bool EsMovimientoValido(int posicion, int direccion)
        {
            switch (direccion)
            {
                case 1:
                    return posicion >= Columnas; // Arriba
                case 2:
                    return posicion < Columnas * (Renglones - 1); // Abajo
                case 3:
                    return posicion % Columnas != 0; // Izquierda
                case 4:
                    return (posicion + 1) % Columnas != 0; // Derecha
                default:
                    return false;
            }
        }

        private void GenerarMaiz()
        {
            for (int i = 0; i < NumMaiz; i++)
            {
                GenerarNuevoMaiz();
            }
        }

        public void MoverPollito(int direccion)
        {
            if (Pollo == null) return;

            int nuevaPosicion = Pollo.Posicion;

            switch (direccion)
            {
                case 1 when nuevaPosicion >= Columnas: nuevaPosicion -= Columnas; break;
                case 2 when nuevaPosicion < (TamañoCorral - Columnas): nuevaPosicion += Columnas; break;
                case 3 when nuevaPosicion % Columnas != 0: nuevaPosicion -= 1; break;
                case 4 when nuevaPosicion % Columnas != (Columnas - 1): nuevaPosicion += 1; break;
            }

            if (Pollos[nuevaPosicion] != null && Pollos[nuevaPosicion].Puntuacion == -10)
            {
                Pollo.Puntuacion++;
                Pollos[nuevaPosicion] = Pollo;
                Pollo.Posicion = nuevaPosicion;

                GenerarNuevoMaiz();
            }
            else if (Pollos[nuevaPosicion] == null)
            {
                Pollos[Pollo.Posicion] = null;
                Pollos[nuevaPosicion] = Pollo;
                Pollo.Posicion = nuevaPosicion;
            }

            OnPropertyChanged(nameof(Pollos));  // Asegurar que la vista se actualice
        }

        private void GenerarNuevoMaiz()
        {
            int nuevaPosicion;
            do
            {
                nuevaPosicion = r.Next(0, TamañoCorral);
            } while (Pollos[nuevaPosicion] != null);

            Pollos[nuevaPosicion] = new PollitoDTO
            {
                Imagen = "🌽",
                Puntuacion = -10,
                Posicion = nuevaPosicion
            };
        }
    }
}