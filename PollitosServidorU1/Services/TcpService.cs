using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace PollitosServidorU1.Services
{
    public class TcpService
    {
        private readonly List<TcpClient> Clientes = new List<TcpClient>();
        private readonly TcpListener listener;
        public event Action<PollitoDTO> PollitoRecibido;
        public event Action<string> ClienteDesconectado;
        public TcpService()
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
            while (true)
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
                Thread.Sleep(1000);
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
                                MessageBox.Show($"Error de deserialización: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error en la conexión con el cliente: {ex.Message}");
                }   
                ClienteDesconectado.Invoke(client.Client.LocalEndPoint.ToString());
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
                    if (c.Client != null && c.Client.RemoteEndPoint != null)
                    {
                        //Notificar al viewmodel para que lo quite de la vista
                        ClienteDesconectado?.Invoke(c.Client.RemoteEndPoint.ToString());
                        //Cerrar la conexion con el cliente
                        c.Client.Close();
                        //lo mejor seria tratar de reconectar el cliente,
                        //pero por simplicidad se elimina
                        Clientes.Remove(c);
                    }
                }
            }
        }
        //Metodo para enviar la lista de pollitos a los clientes
        public void RetransmitirLista(List<PollitoDTO> lista)
        {
            // Recorremos la lista de clientes
            for (int i = 0; i < Clientes.Count; i++)
            {
                // Si el cliente está conectado
                if (Clientes[i].Connected)
                {
                    var json = JsonConvert.SerializeObject(lista);
                    // Serializar el objeto
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    try
                    {
                        if (Clientes[i].Connected)
                        {
                            Clientes[i].Client.Send(buffer);
                        }
                    }
                    catch (Exception)
                    {
                        if (Clientes[i].Client != null && Clientes[i].Client.RemoteEndPoint != null)
                        {
                            //Notificar al viewmodel para que lo quite de la vista
                            ClienteDesconectado?.Invoke(Clientes[i].Client.RemoteEndPoint.ToString());
                            //Cerrar la conexion con el cliente
                            Clientes[i].Client.Close();
                            //lo mejor seria tratar de reconectar el cliente,
                            //pero por simplicidad se elimina
                            Clientes.Remove(Clientes[i]);
                        }
                    }
                }

            }
        }
    }
}