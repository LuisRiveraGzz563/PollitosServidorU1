using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PollitosClienteU1.Models;
using PollitosClienteU1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly TcpService _servidor = new TcpService();
        private readonly string[] _images = { "🐥", "🌽" };
        private readonly Timer _contador;
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        private int Tamaño => Columnas * Renglones;

        [ObservableProperty]
        private bool isConnected;

        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
        }

        public ConexionModel Conexion { get; set; } = new ConexionModel();
        public PollitoDTO Pollito { get; set; }
        public Corral Corral { get; } = new Corral(100);
        public ICommand ConectarCommand { get; }
        public MainViewModel()
        {
            ConectarCommand = new RelayCommand(Conectar);
            _servidor.PollitoRecibido += Cliente_PollitoRecibido;
            _servidor.MaizRecibido += Cliente_MaizRecibido;
            _contador = new Timer(EliminarMaiz, null, 1000, 1000);
        }
        private void Cliente_MaizRecibido(List<PollitoDTO> list)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                foreach (var pollito in list)
                {
                    Corral.Pollos[pollito.Posicion] = pollito;
                }
            });
        }
        private void Cliente_PollitoRecibido(PollitoDTO dto)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var polloEnTablero = Corral.Pollos.FirstOrDefault(x => x?.Nombre == dto.Nombre);
                if (polloEnTablero == null)
                {
                    ManejarNuevoPollito(dto);
                }
                else if (dto.Direccion > 0 && dto.Direccion < 5 && EsMovimientoValido(dto.Posicion, dto.Direccion, Columnas, Tamaño))
                {
                    //Si el cliente esta sincronizado con el servidor
                    if (polloEnTablero.Posicion == dto.Posicion)
                    {
                        MoverPollito(polloEnTablero.Posicion, dto.Direccion);
                    }
                    //Si el cliente no esta sincronizado con el servidor
                    else
                    {

                        Corral.Pollos[dto.Posicion] = polloEnTablero;
                        Corral.Pollos[polloEnTablero.Posicion] = null;
                        polloEnTablero.Posicion = dto.Posicion;
                        polloEnTablero.Puntuacion = dto.Puntuacion;
                    }
                }
            });
        }
        private static bool EsMovimientoValido(int posicion, int direccion, int columnas, int tamaño)
        {
            switch (direccion)
            {
                case 1:
                    return posicion >= columnas;
                case 2:
                    return posicion < tamaño - columnas;
                case 3:
                    return posicion % columnas != 0;
                case 4:
                    return (posicion + 1) % columnas != 0;
                default:
                    return false;
            }
        }
        private void ManejarNuevoPollito(PollitoDTO dto)
        {
            //si es un maiz colocarlo en su respectiva posicion
            if (dto.Imagen != "🌽")
            {
                Corral.Pollos[dto.Posicion] = dto;
                Pollito.Posicion = dto.Posicion;
            }
            else
            {
                //Buscar primera posicion disponible
                for (int i = 0; i < Corral.Pollos.Count; i++)
                {
                    //Si esta disponible
                    if (Corral.Pollos[i] == null)
                    {
                        Corral.Pollos[i] = dto;
                        dto.Posicion = i;
                        //Actualizar la posicion del Modelo actual solo si es el cliente
                        if (dto.Cliente == Pollito.Cliente)
                        {
                            Pollito.Posicion = i;
                        }
                        break;
                    }
                }
            }
        }
        public void MoverPollito(int posicion, int direccion)
        {
            //Si se mueve un objeto que no existe no hacer nada
            if (Corral.Pollos[posicion] == null) return;

            int nuevaPosicion;
            //Asigna la nueva posicion utilizando la direccion
            switch (direccion)
            {
                case 1 when posicion >= Columnas:
                    nuevaPosicion = posicion - Columnas;
                    break;
                case 2 when posicion < Tamaño - Columnas:
                    nuevaPosicion = posicion + Columnas;
                    break;
                case 3 when posicion % Columnas != 0:
                    nuevaPosicion = posicion - 1;
                    break;
                case 4 when posicion % Columnas != (Columnas - 1):
                    nuevaPosicion = posicion + 1;
                    break;
                default:
                    nuevaPosicion = posicion;
                    break;
            }
            //Si la posicion es igual, no hacer nada
            if (nuevaPosicion == posicion) return;
            //si "comio" un maiz se aumenta la puntuacion
            if (Corral.Pollos[nuevaPosicion]?.Imagen == "🌽")
            {
                Corral.Pollos[posicion].Puntuacion++;
            }
            //se crea una copia en la nueva posicion
            Corral.Pollos[nuevaPosicion] = Corral.Pollos[posicion];
            //Se actualiza la posicion del objeto
            Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
            //Se "Elimina" la version original
            Corral.Pollos[posicion] = null;

            if (Corral.Pollos[nuevaPosicion].Cliente == Pollito.Cliente)
            {
                Pollito.Posicion = nuevaPosicion;
                Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
            }
        }
        private void Conectar()
        {
            try
            {
                if (!IPAddress.TryParse(Conexion.IP, out _))
                {
                    MessageBox.Show("La dirección IP es incorrecta");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Conexion.Nombre))
                {
                    MessageBox.Show("Ingrese un Nombre");
                    return;
                }

                if (!_servidor.IsConnected())
                {
                    _servidor.Conectar(Conexion.IP);
                }

                if (_servidor.IsConnected())
                {
                    Pollito = new PollitoDTO { Nombre = Conexion.Nombre, Imagen = "🐥" };
                    _servidor.EnviarPollito(Pollito);
                }

                IsConnected = _servidor.IsConnected();
                if (!IsConnected)
                {
                    MessageBox.Show("Ya existe otro usuario con el mismo nombre");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        public void EnviarMovimiento(int num)
        {
            if (_servidor.IsConnected())
            {
                Pollito.Direccion = num;
                _servidor.EnviarPollito(Pollito);
            }
        }
        private void EliminarMaiz(object state)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                var maizParaEliminar = Corral.Pollos
                    .Where(x => x != null && x.Imagen == _images[1])
                    .ToList();

                foreach (var maiz in maizParaEliminar)
                {
                    if (maiz.Duracion == 0)
                    {
                        Corral.Pollos[maiz.Posicion] = null;
                    }
                    else
                    {
                        maiz.Duracion--;
                    }
                }
            });
        }
    }
}
