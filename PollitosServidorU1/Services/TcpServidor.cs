using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PollitosServidorU1.Services
{
    public class TcpServidor
    {
        private readonly List<TcpClient> Clientes = new List<TcpClient>();
        private readonly TcpListener listener;
        public event Action<PollitoDTO> PollitoRecibido;
        public event Action<string> ClienteDesconectado;
        public TcpServidor()
        {
            // Inicializamos el objeto listener con la IP Any y el puerto 5000
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            // Recibimos clientes en un hilo aparte
            Thread recibirClientes = new Thread(RecibirClientes) { IsBackground = true };
            recibirClientes.Start();
        }
        private void RecibirClientes()
        {
            //Limitar la cantidad de usuarios 
            while (Clientes.Count<=5)
            {
                // Aceptamos clientes TCP
                TcpClient tcpClient = listener.AcceptTcpClient();
                // Agregamos el cliente a la lista de clientes
                Clientes.Add(tcpClient);
                // Creamos un hilo para escuchar a los clientes
                Thread t = new Thread(Escuchar)
                {
                    IsBackground = true
                };
                t.Start(tcpClient);
            }
        }
        private void Escuchar(object tcpClient)
        {
            if (tcpClient != null)
            {
                var client = (TcpClient)tcpClient;
                var stream = client.GetStream();
                try
                {
                    while (client.Connected)
                    {
                        if (stream.DataAvailable)
                        {
                            byte[] buffer = new byte[client.Available];
                            stream.Read(buffer, 0, buffer.Length);
                            var json = Encoding.UTF8.GetString(buffer);
                            try
                            {
                                // Deserializamos el JSON a un objeto PollitoDTO
                                var pollito = JsonConvert.DeserializeObject<PollitoDTO>(json);

                                if (pollito != null)
                                {
                                    PollitoRecibido?.Invoke(pollito);
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.WriteLine($"Error de deserialización: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error en la conexión con el cliente: {ex.Message}");
                }
            }
        }

        //Metodo para enviar la lista de pollitos a los clientes
        public void Retransmitir(PollitoDTO pollo)
        {
            // Serializar el objeto
            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pollo));
            // Recorremos la lista de clientes
            foreach (var c in Clientes)
            {
                try
                {
                    if (c.Connected)
                    {
                        c.Client.Send(buffer);
                    }
                }
                catch (Exception)
                {
                    DesconectarCliente(c.Client.RemoteEndPoint.ToString());
                }
            }
        }
        //Metodo para enviar la lista de pollitos a los clientes
        public void RetransmitirLista(List<PollitoDTO> lista)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lista));
            var count = Clientes.Count;
            if (count>0)
            {
                // Recorremos la lista de clientes
                foreach (var c in Clientes.ToList())
                {
                    try
                    {
                        if (c.Connected)
                        {
                            c.Client.Send(buffer);
                        }
                    }
                    catch (Exception)
                    {
                        DesconectarCliente(c.Client.RemoteEndPoint.ToString());
                    }
                }
            }
        }
        public void DesconectarCliente(string cliente)
        {
            var c = Clientes.FirstOrDefault(x => x.Client.RemoteEndPoint.ToString() == cliente); 
            if (c != null)
            {
                //Notificar al viewmodel para que lo quite de la vista
                ClienteDesconectado?.Invoke(c.Client.RemoteEndPoint.ToString());
                c.Close();
                //lo mejor seria tratar de reconectar el cliente,
                //pero por simplicidad se elimina
                Clientes.Remove(c);
            }
        }
    }
}