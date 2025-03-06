using CommunityToolkit.Mvvm.Input;
using PollitosClienteU1.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PollitosClienteU1.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        private readonly TcpService Servidor = new TcpService();
        public bool IsConnected { get; set; } = false;
        public string Nombre { get; set; }
        public string IP { get; set; }
        public ICommand ConectarCommand { get; set; }

        public MainViewModel()
        {
            ConectarCommand = new RelayCommand(Conectar);
            
        }

        //Metodo para conectar el cliente con el servidor
        private void Conectar()
        {
            try
            {
                //Llamamos al metodo Conectar del servicio TcpService
                Servidor.Conectar(IP);

                //Verificamos si el cliente esta conectado
                IsConnected = Servidor.IsConnected();
                //Notificamos a la vista que la propiedad IsConnected ha cambiado
                OnPropertyChanged(nameof(IsConnected));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar con el servidor:" + ex.Message);
            }
        }

        void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }
}
