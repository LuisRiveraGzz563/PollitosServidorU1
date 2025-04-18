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
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();
            Thread recibirClientes = new Thread(RecibirClientes) { IsBackground = true };
            recibirClientes.Start();
        }
        private void RecibirClientes()
        {
            // Aceptar hasta 5 clientes
            while (Clientes.Count <= 5)
            {
                // Aceptar un cliente
                TcpClient tcpClient = listener.AcceptTcpClient();
                // Agregar el cliente a la lista
                Clientes.Add(tcpClient);
                // Iniciar un hilo para escuchar al cliente
                new Thread(Escuchar) { IsBackground = true }.Start(tcpClient);
            }
        }
        private void Escuchar(object tcpClient)
        {
            // verificamos si el cliente es un TcpClient
            if (tcpClient is TcpClient client)
            {
                // Obtenemos el stream del cliente  
                var stream = client.GetStream();

                try
                {
                    // Verificamos si el cliente sigue conectado
                    while (client.Connected)
                    {
                        // Verificamos si hay datos disponibles
                        if (stream.DataAvailable)
                        {
                            // creamos un buffer para leer los datos
                            byte[] buffer = new byte[client.Available];
                            // Leemos los datos del stream
                            stream.Read(buffer, 0, buffer.Length);
                            // convertimos el buffer a string
                            var json = Encoding.UTF8.GetString(buffer);
                            // intentamos deserializar el json a un objeto PollitoDTO
                            try
                            {
                                // deserializamos el json a un objeto PollitoDTO
                                var pollito = JsonConvert.DeserializeObject<PollitoDTO>(json);
                                // si el pollito no es nulo, invocamos el evento PollitoRecibido
                                if (pollito != null)
                                {
                                    PollitoRecibido?.Invoke(pollito);
                                }
                            }
                            // si hay un error de deserialización, lo capturamos
                            catch (JsonException ex)
                            {
                                Debug.WriteLine($"Error de deserialización: {ex.Message}");
                            }
                        }
                    }
                }
                // si hay un error al leer del stream, lo capturamos
                catch (SocketException ex)
                {
                    Debug.WriteLine($"Error de socket: {ex.Message}");
                }
                // si hay un error al aceptar el cliente, lo capturamos
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error en la conexión con el cliente: {ex.Message}");
                }
            }
        }
        public void Retransmitir(object pollito, string cliente = null)
        {
            // Serializamos el objeto a JSON
            var json = JsonConvert.SerializeObject(pollito);
            var buffer = Encoding.UTF8.GetBytes(json);

            // Si se especifica un cliente, enviamos solo a ese cliente
            if (!string.IsNullOrWhiteSpace(cliente))
            {
                var c = Clientes.FirstOrDefault(x => x.Client.RemoteEndPoint.ToString() == cliente);
                if (c != null)
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
            else
            {
                // Si no se especifica cliente, lo enviamos a todos los conectados
                var clientesAEliminar = new List<string>();

                foreach (var c in Clientes.ToList())
                {
                    try
                    {
                        if (c.Connected)
                        {
                            c.Client.Send(buffer);
                        }
                    }
                    catch
                    {
                        clientesAEliminar.Add(c.Client.RemoteEndPoint.ToString());
                    }
                }

                // Desconectamos a los clientes que fallaron
                foreach (var id in clientesAEliminar)
                {
                    DesconectarCliente(id);
                }
            }
        }
        public void DesconectarCliente(string cliente)
        {
            var c = Clientes.FirstOrDefault(x => x.Client?.RemoteEndPoint?.ToString() == cliente);

            if (c != null)
            {
                try
                {
                    // Invocar evento para eliminar al cliente del tablero
                    ClienteDesconectado?.Invoke(cliente);

                    // Cerrar el stream y el cliente
                    if (c.Connected)
                    {
                        // Cerramos el stream
                        c.GetStream()?.Close();
                    }
                    // Cerramos el cliente
                    c.Close();
                    // desconectamos el cliente
                    c.Dispose();
                }
                catch (Exception ex)
                {
                    // Puedes loguear o manejar el error si algo falla
                    Console.WriteLine($"Error al desconectar cliente {cliente}: {ex.Message}");
                }
                finally
                {
                    // Eliminar de la lista pase lo que pase
                    Clientes.Remove(c);
                }
            }
        }

    }
}