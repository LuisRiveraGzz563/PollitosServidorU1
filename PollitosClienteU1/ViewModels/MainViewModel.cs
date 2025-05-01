using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PollitosClienteU1.Models;
using PollitosClienteU1.Services;
using System;
using System.IO;
using System.Media;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly TcpService _servidor = new TcpService();
        private readonly string[] _images = { "🐥", "🌽" };
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        private int Tamaño => Columnas * Renglones;

        [ObservableProperty]

        private bool isConnected;
        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                isConnected = value;
                OnPropertyChanged(nameof(isConnected));
            }
        }
        public ConexionModel Conexion { get; set; } = new ConexionModel();
        // Este pollito es el cliente, cada vez que se cambie la posicion en el servidor se debe actualizar localmente
        public PollitoDTO Pollito { get; set; }
        private readonly SoundPlayer _sonidoMovimiento = new SoundPlayer("Sonidos/pollito.wav");
        private readonly SoundPlayer _sonidoComer = new SoundPlayer("Sonidos/Comida.wav");
        // Este es el tablero
        public Corral Corral { get; } = new Corral(100);
        public ICommand ConectarCommand { get; }
        public MainViewModel()
        {
            ConectarCommand = new RelayCommand(Conectar);
        }
        private async void Conectar()
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
                _servidor.Conectar(Conexion.IP);
                Pollito = new PollitoDTO { Nombre = Conexion.Nombre, Imagen = "🐥" };
                _servidor.EnviarPollito(Pollito);
                IsConnected = true;
                // Iniciar escucha en segundo plano
                await Task.Run(() => _servidor.PollitoRecibido += Cliente_PollitoRecibido);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void Cliente_PollitoRecibido(PollitoDTO dto)
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                //Eliminacion de pollito
                if (dto!=null && dto.Imagen == null && dto.Posicion >= 0 && dto.Posicion < Corral.Pollos.Count)
                {
                    Corral.Pollos[dto.Posicion] = null;
                    return;
                }
                bool esCliente = dto.Cliente == Pollito.Cliente;
                // Detectar si había maíz en la posición destino (antes de mover)
                bool comio = Corral.Pollos[dto.Posicion]?.Imagen == "🌽";
                // Movimiento del pollito propio
                if (esCliente)
                {
                    int posAnterior = Pollito.Posicion;

                    // Eliminar al pollito de la posición anterior
                    if (posAnterior >= 0 && posAnterior < Corral.Pollos.Count)
                        Corral.Pollos[posAnterior] = null;

                    // Colocar en nueva posición
                    Corral.Pollos[dto.Posicion] = dto;

                    // Actualizar datos del pollito
                    Pollito.Posicion = dto.Posicion;
                    Pollito.Puntuacion = dto.Puntuacion;

                    // Reproducir sonido según acción
                    if (comio)
                        _sonidoComer.Play();
                    else
                        _sonidoMovimiento.Play();
                }
                else
                {
                    // Otro jugador: solo colocar en el tablero
                    Corral.Pollos[dto.Posicion] = dto;
                }
            });
        }

        private bool EsMovimientoValido(int posicion, int direccion)
        {
            int nuevaPosicion = posicion;
            switch (direccion)
            {
                case 1: nuevaPosicion = posicion >= Columnas ? posicion - Columnas : posicion; break;
                case 2: nuevaPosicion = posicion < Tamaño - Columnas ? posicion + Columnas : posicion; break;
                case 3: nuevaPosicion = posicion % Columnas != 0 ? posicion - 1 : posicion; break;
                case 4: nuevaPosicion = posicion % Columnas != (Columnas - 1) ? posicion + 1 : posicion; break;
            }

            if (nuevaPosicion >= 0)
            {
                var pollo = Corral.Pollos[nuevaPosicion];
                return pollo == null || pollo.Imagen == _images[1];
            }
            return false;
        }
        public void EnviarMovimiento(int direccion)
        {
            if (_servidor.IsConnected() && EsMovimientoValido(Pollito.Posicion, direccion))
            {
                Pollito.Direccion = direccion;
                _servidor.EnviarPollito(Pollito);
            }
        }
    }
}
