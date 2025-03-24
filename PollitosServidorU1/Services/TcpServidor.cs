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
            while (Clientes.Count <= 5)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();
                Clientes.Add(tcpClient);
                Thread t = new Thread(Escuchar) { IsBackground = true };
                t.Start(tcpClient);
            }
        }
        private void Escuchar(object tcpClient)
        {
            if (tcpClient is TcpClient client)
            {
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

        public void Retransmitir(object pollito)
        {
            var json = JsonConvert.SerializeObject(pollito);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

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
        public void Retransmitir(object pollito, string cliente = null)
        {
            var json = JsonConvert.SerializeObject(pollito);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            foreach (var c in Clientes.ToList())
            {
                try
                {
                    if (c.Connected && c.Client.RemoteEndPoint.ToString() == cliente)
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

        public void DesconectarCliente(string cliente)
        {
            var c = Clientes.FirstOrDefault(x => x.Client.RemoteEndPoint.ToString() == cliente);
            if (c != null)
            {
                ClienteDesconectado?.Invoke(cliente);
                c.Close();
                Clientes.Remove(c);
            }
        }
    }
}