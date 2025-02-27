using Newtonsoft.Json;
using PollitosServidorU1.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PollitosServidorU1.Services
{
    public class TcpService
    {
        //Creamos un objeto de tipo TcpListener
        private readonly TcpListener listener;
        private readonly List<TcpClient> Clientes = new List<TcpClient>();
        public event Action<PollitoDTO> PollitoRecibido;
        public TcpService()
        {
            //Inicializamos el objeto listener con la IP Any y el puerto 5000
            listener = new TcpListener(IPAddress.Any, 5000);
            //Iniciamos el listener
            listener.Start();
            //Recibimos clientes
            RecibirClientes();
        }

        private void RecibirClientes()
        {
            TcpClient tcpClient = listener.AcceptTcpClient();
            //Crear un hilo para escuchar a los clientes
            Thread t = new Thread(Escuchar);
        }

        private void Escuchar(object tcpClient)
        {
            if (tcpClient != null)
            {
                var client = (TcpClient)tcpClient;
                //Agregamos el cliente a la lista de clientes
                Clientes.Add(client);
                while (client.Connected)
                {
                    if (client.Available > 0)
                    {
                        //Creamos un buffer para recibir los datos
                        byte[] buffer = new byte[client.Available];
                        //Leemos los datos del cliente y guardar los datos en el buffer
                        client.GetStream().Read(buffer, 0, buffer.Length);
                        //Convertimos los datos a string
                        var json = Encoding.UTF8.GetString(buffer);
                        //Convertimos el json a un objeto
                        var pollito = JsonConvert.DeserializeObject<PollitoDTO>(json);

                        if (pollito != null)
                        {
                            //Invocamos el evento PollitoRecibido
                            PollitoRecibido?.Invoke(pollito);
                            try
                            {
                                //Enviamos la lista de clientes a todos los clientes
                                foreach (var c in Clientes)
                                {
                                    c?.Client.Send(buffer);
                                }
                            }
                            catch (Exception)
                            {
                                //Si hay un error, eliminamos el cliente de la lista
                                Clientes.Remove(client);
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
