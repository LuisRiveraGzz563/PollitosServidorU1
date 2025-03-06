using Newtonsoft.Json;
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
        // Usamos ConcurrentBag para manejar clientes en un entorno multihilo de manera eficiente
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
                                Console.WriteLine($"Error de deserialización: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en la conexión con el cliente: {ex.Message}");
                }
                finally
                {
                    // Cuando termina la comunicación con el cliente, lo eliminamos de la lista
                    client.Close();
                    Clientes.Remove(client);  // Elimina el cliente de la lista concurrente
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
                        c.Client.Send( buffer);
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
        public void Retransmitir(List<PollitoDTO> lista)
        {
            // Recorremos la lista de pollitos
            for (int i = 0; i < lista.Count; i++)
            {
                // Obtenemos el cliente correspondiente al pollito
                var client = Clientes.Find(c => c.Client.RemoteEndPoint.ToString() == lista[i].Cliente);
                // Si el cliente está conectado
                if (client != null && client.Connected)
                {
                    // Obtenemos el stream de red del cliente
                    var stream = client.GetStream();
                    // Enviamos el mensaje a través del stream
                    byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(lista[i]));
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}