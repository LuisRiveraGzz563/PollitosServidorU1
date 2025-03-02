using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PollitosServidorU1.Services
{
    public class TcpService
    {
        // Usamos ConcurrentBag para manejar clientes en un entorno multihilo de manera eficiente
        private readonly ConcurrentBag<TcpClient> Clientes = new ConcurrentBag<TcpClient>();
        private readonly TcpListener listener;
        public event Action<PollitoDTO> PollitoRecibido;

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
                                    // Enviamos los datos a todos los clientes conectados
                                    foreach (var c in Clientes)
                                    {
                                        // Verificamos si el cliente está conectado antes de enviarle datos
                                        if (c.Connected)
                                        {
                                            c.GetStream().Write(buffer, 0, buffer.Length);
                                        }
                                    }
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
                    Clientes.TryTake(out var removedClient);  // Elimina el cliente de la lista concurrente
                }
            }
        }
    }
}
