using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PollitosClienteU1.Services
{
    public class TcpService
    {
        private readonly TcpClient Client;
        public TcpService()
        {
            Client = new TcpClient();
            
        }

        //Este metodo se encarga de conectar el cliente con el servidor
        public void Conectar(string IP)
        {
            try
            {
                Client.Connect(IP, 5000);
            }
            catch (Exception)
            {
            }
        }

        //Este metodo se encarga de desconectar el cliente del servidor
        public void Desconectar()
        {
            Client.Close();
        }

        //Este metodo sirve para verificar si el cliente esta conectado
        public bool IsConnected()
        {
            return Client.Connected;
        }


    }
}
