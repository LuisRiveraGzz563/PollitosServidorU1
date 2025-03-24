using CommunityToolkit.Mvvm.ComponentModel;
using PollitosServidorU1.Models;
using PollitosServidorU1.Services;
using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PollitosServidorU1.ViewModels
{
    public class CorralViewModel : ObservableObject
    {
        #region Propiedades, servicios y Corral
        private readonly string[] Images = new string[2] { "🐥", "🌽" };
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        public int NumMaiz { get; set; } = 5;
        public int TamañoCorral => Columnas * Renglones;
        public Corral Corral { get; set; }
        private static readonly TcpServidor Servidor = new TcpServidor();
        private readonly Random r = new Random();
        private readonly Timer Contador;
        #endregion
        public CorralViewModel()
        {
            Corral = new Corral(TamañoCorral);
            GenerarMaiz();
            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
            Servidor.ClienteDesconectado += Servidor_ClienteDesconectado;
            Contador = new Timer(EliminarMaiz, null, 1000, 1000);
        }
        #region Eventos
        private void Servidor_ClienteDesconectado(string cliente)
        {
            var pollo = Corral.Pollos.FirstOrDefault(x => x != null && x.Cliente == cliente);
            if (pollo != null)
            {
                Corral.Pollos[pollo.Posicion] = null;
                Servidor.Retransmitir(Corral.Pollos.Where(x => x != null).ToList());
            }
        }
        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                var polloEnTablero = Corral.Pollos.FirstOrDefault(x => x != null && x.Nombre == dto.Nombre);
                if (polloEnTablero == null)
                {
                    ManejarNuevoPollito(dto);
                }
                else if (dto.Direccion != 0 && EsMovimientoValido(dto.Posicion, dto.Direccion))
                {
                    MoverPollito(polloEnTablero.Posicion, dto.Direccion);
                }
            }));
        }
        private void ManejarNuevoPollito(PollitoDTO dto)
        {
            var polloEnTablero = Corral.Pollos.FirstOrDefault(x => x != null && x.Nombre == dto.Nombre);
            if (polloEnTablero != null && polloEnTablero.Cliente != dto.Cliente)
            {
                Servidor.DesconectarCliente(dto.Cliente);
                return;
            }
            if (polloEnTablero == null)
            {
                for (int i = 0; i < Corral.Pollos.Count; i++)
                {
                    if (Corral.Pollos[i] == null)
                    {
                        Corral.Pollos[i] = dto;
                        dto.Posicion = i;
                        Servidor.Retransmitir(dto);
                        Servidor.Retransmitir(Corral.Pollos.Where(x => x != null && x.Cliente != dto.Cliente).ToList(), dto.Cliente);
                        break;
                    }
                }
            }
            else if (EsMovimientoValido(dto.Posicion, dto.Direccion))
            {
                MoverPollito(dto.Posicion, dto.Direccion);
            }
        }
        private bool EsMovimientoValido(int posicion, int direccion)
        {
            switch (direccion)
            {
                case 1:
                    return posicion >= Columnas;
                case 2:
                    return posicion < Columnas * (Renglones - 1);
                case 3:
                    return posicion % Columnas != 0;
                case 4:
                    return (posicion + 1) % Columnas != 0;
                default:
                    return false;
            }
        }
        public void MoverPollito(int posicion, int direccion)
        {
            if (Corral.Pollos[posicion] == null) return;

            int nuevaPosicion;
            switch (direccion)
            {
                case 1:
                    nuevaPosicion = posicion >= Columnas ? posicion - Columnas : posicion;
                    break;
                case 2:
                    nuevaPosicion = posicion < (TamañoCorral - Columnas) ? posicion + Columnas : posicion;
                    break;
                case 3:
                    nuevaPosicion = posicion % Columnas != 0 ? posicion - 1 : posicion;
                    break;
                case 4:
                    nuevaPosicion = posicion % Columnas != (Columnas - 1) ? posicion + 1 : posicion;
                    break;
                default:
                    nuevaPosicion = posicion;
                    break;
            }
            if (nuevaPosicion == posicion) return;
            if (Corral.Pollos[nuevaPosicion] != null && Corral.Pollos[nuevaPosicion].Imagen == "🌽")
            {
                Corral.Pollos[posicion].Puntuacion++;
                GenerarNuevoMaiz();
            }
            Corral.Pollos[nuevaPosicion] = Corral.Pollos[posicion];
            Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
            Corral.Pollos[nuevaPosicion].Direccion = direccion;
            Corral.Pollos[posicion] = null;

            Servidor.Retransmitir(Corral.Pollos[nuevaPosicion]);
            var tablero = Corral.Pollos.Where(x => x != null && x.Cliente != Corral.Pollos[nuevaPosicion].Cliente).ToList();
            Servidor.Retransmitir(tablero, Corral.Pollos[nuevaPosicion].Cliente);
        }
        #endregion
        #region Generacion y Eliminacion de Maiz
        private void GenerarMaiz()
        {
            for (int i = 0; i < NumMaiz; i++)
            {
                GenerarNuevoMaiz();
            }
        }
        private void GenerarNuevoMaiz()
        {
            int nuevaPosicion;
            do
            {
                nuevaPosicion = r.Next(0, TamañoCorral);
            }
            while (Corral.Pollos[nuevaPosicion] != null);

            Corral.Pollos[nuevaPosicion] = new PollitoDTO
            {
                Puntuacion = -10,
                Imagen = Images[1],
                Posicion = nuevaPosicion,
                Duracion = 5
            };
            Servidor.Retransmitir(Corral.Pollos[nuevaPosicion]);
        }
        private void EliminarMaiz(object state)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                var maizParaEliminar = Corral.Pollos
                    .Where(x => x != null && x.Imagen == Images[1] && x.Duracion == 0)
                    .ToList();

                foreach (var maiz in maizParaEliminar)
                {
                    Corral.Pollos[maiz.Posicion] = null;
                    GenerarNuevoMaiz();
                }
            }));
        }
        #endregion
    }
}
