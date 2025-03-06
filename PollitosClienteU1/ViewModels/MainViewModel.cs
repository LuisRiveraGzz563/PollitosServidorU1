using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PollitosClienteU1.Models;
using PollitosClienteU1.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public readonly TcpService Servidor = new TcpService();
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
            Servidor.PollitoRecibido += Servidor_ListaRecibida;
        }
        #region Tablero 
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        private int Tamaño => Columnas * Renglones;
        public Corral Corral { get; set; }
        
        private void Servidor_ListaRecibida(List<PollitoDTO> lista)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < lista.Count; i++)
                {
                    var anterior = Corral.Pollos.FirstOrDefault(x =>
                    x != null && x.Cliente == lista[i].Cliente && x.Posicion == lista[i].Posicion);

                    //Si no esta en pantalla
                    if(anterior == null)
                    {
                        if (Corral.Pollos[lista[i].Posicion] != null)
                        {
                            foreach (var item in Corral.Pollos)
                            {
                                if (item == null)
                                {
                                    // Agregarlo al corral
                                    lista[i].Posicion = Corral.Pollos.IndexOf(item);
                                    // Agregarlo al corral
                                    Corral.Pollos[lista[i].Posicion] = lista[i];
                                    break;
                                }
                            }
                        }
                        // Agregarlo al corral
                        Corral.Pollos[lista[i].Posicion] = lista[i];
                    }
                    //Si ya estaba
                    else
                    {
                        if(EsMovimientoValido(lista[i].Posicion, lista[i].Direccion))
                        {
                            MoverPollito(lista[i].Posicion, lista[i].Direccion);
                        }
                    }
                }
            });
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
        public void MoverPollito(int posicion, int direccion)
        {
            // Si la posición no está vacía, no hacer nada
            if (Corral.Pollos[posicion] != null) return;
            // variable para la nueva posición
            int nuevaPosicion = posicion;
            //Asignar la nueva posicion dependiendo de la direccion
            switch (direccion)
            {
                // Arriba
                case 1 when nuevaPosicion >= Columnas: nuevaPosicion -= Columnas; break;
                // Abajo
                case 2 when nuevaPosicion < (Tamaño - Columnas): nuevaPosicion += Columnas; break;
                // Izquierda
                case 3 when nuevaPosicion % Columnas != 0: nuevaPosicion -= 1; break;
                // Derecha
                case 4 when nuevaPosicion % Columnas != (Columnas - 1): nuevaPosicion += 1; break;
            }
            #endregion
        }
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
                //Llamamos al metodo Conectar del servicio TcpService
                Servidor.Conectar(Conexion.IP);

                //Verificamos si el cliente esta conectado
                IsConnected = Servidor.IsConnected();
                //Si esta conectado enviamos un pollito
                if (IsConnected)
                {
                    //Crear un pollito para enviarlo por primera vez
                    PollitoDTO pollito = new PollitoDTO()
                    {
                        Nombre = Conexion.Nombre,
                        Posicion = 0,
                        Puntuacion = 0,
                        Direccion = 0,
                        Imagen = "🐥"
                    };

                    Servidor.EnviarPollito(pollito);

                }
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con el servidor:" + ex.Message);
            }
        }
        #endregion
    }
}
