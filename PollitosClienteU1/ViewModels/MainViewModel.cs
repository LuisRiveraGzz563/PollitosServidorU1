using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PollitosClienteU1.Models;
using PollitosClienteU1.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public readonly TcpService Servidor = new TcpService();
        public PollitoDTO Pollito { get; set; }
        #region INotifyPropertyChanged
        void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
        public MainViewModel()
        {
            Corral = new Corral(Tamaño);
            ConectarCommand = new RelayCommand(Conectar);
            Servidor.ListaRecibida += Servidor_ListaRecibida;
        }
        #region Tablero 
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        private int Tamaño => Columnas * Renglones;
        public Corral Corral { get; set; }
        private void Servidor_ListaRecibida(List<PollitoDTO> lista)
        {
            Application.Current.Dispatcher?.Invoke(() =>
            {
                // Limpiar el corral antes de actualizarlo
                Corral.Pollos.Clear();
                // Inicializar el corral con espacios vacíos (null)
                for (int i = 0; i < Tamaño; i++)
                {
                    Corral.Pollos.Add(null);
                }
                // Agregar los pollitos al corral
                foreach (var pollito in lista)
                {
                    if (pollito.Posicion >= 0 && pollito.Posicion < Tamaño)
                    {
                        if(pollito.Cliente == Pollito.Cliente)
                        {
                            Pollito.Posicion = pollito.Posicion;
                        }
                        Corral.Pollos[pollito.Posicion] = pollito;
                    }
                }
            });
        }
        #endregion
        #region ConexionView
        #region Propiedades
        [ObservableProperty]
        public bool IsConnected { get; set; } = false;
        public ConexionModel Conexion { get; set; } = new ConexionModel();
        public ICommand ConectarCommand { get; set; }
        #endregion
        //Metodo para conectar el cliente con el servidor
        private void Conectar()
        {
            try
            {
                bool IsValid = IPAddress.TryParse(Conexion.IP, out IPAddress ip);
                if (!IsValid)
                {
                    MessageBox.Show("La direccion IP es incorrecta");
                }
                if (string.IsNullOrWhiteSpace(Conexion.Nombre))
                {
                    MessageBox.Show("Ingrese un Nombre");
                }
                //Verificamos si el cliente esta conectado
                IsConnected = Servidor.IsConnected();
                if (!IsConnected)
                {
                    //Llamamos al metodo Conectar del servicio TcpService
                    Servidor.Conectar(Conexion.IP);
                }
                //Si esta conectado enviamos un pollito
                if (Servidor.IsConnected())
                {
                    //Crear un pollito para enviarlo por primera vez
                    Pollito = new PollitoDTO()
                    {
                        Nombre = Conexion.Nombre,
                        Imagen = "🐥"
                    };
                    Servidor.EnviarPollito(Pollito);
                }
                IsConnected = Servidor.IsConnected();
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con el servidor:" + ex.Message);
            }
        }
        #endregion
        #region Movimiento  
        public void EnviarMovimiento(int num)
        {
            //Asignar la direccion al pollito
            Pollito.Direccion = num;
            Servidor.EnviarPollito(Pollito);

        }
        #endregion
    }
}
