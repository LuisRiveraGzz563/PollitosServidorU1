using Newtonsoft.Json;
using PollitosClienteU1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace PollitosClienteU1.Services
{
    public class TcpService
    {
        protected readonly TcpClient tcpClient;
        public Action<List<PollitoDTO>> PollitoRecibido;
        public TcpService()
        {
            tcpClient = new TcpClient();
        }

        #region Metodos de conexion
        //Este metodo se encarga de conectar el cliente con el servidor
        public void Conectar(string IP)
        {
            try
            {
                tcpClient.Connect(IP, 5000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Este metodo se encarga de desconectar el cliente del servidor
        public void Desconectar()
        {
            if (!tcpClient.Connected)
            {
                tcpClient.Close();
            }
        }

        //Este metodo sirve para verificar si el cliente esta conectado
        public bool IsConnected()
        {
            return tcpClient.Connected;
        }
        #endregion

        //Metodo para enviar un polllito al servidor
        public void EnviarPollito(PollitoDTO dto)
        {
            try
            {
                if (dto.Cliente == null)
                {
                    // Asignamos la IP del cliente al objeto PollitoDTO
                    dto.Cliente = ObtenerIPLocal();
                }
                // Serializamos el objeto a JSON
                var json = JsonConvert.SerializeObject(dto);
                // Convertimos el JSON a bytes
                var buffer = Encoding.UTF8.GetBytes(json);
                // Enviamos los bytes al servidor
                tcpClient.GetStream().Write(buffer, 0, buffer.Length);
                Thread hilo = new Thread(Escuchar) { IsBackground = true };
                hilo.Start();
            }
            catch (SocketException)
            {
                Desconectar();
            }
        }
        private void Escuchar()
        {
            if (tcpClient != null)
            {
                var client = tcpClient;
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
                                var pollitos = JsonConvert.DeserializeObject<List<PollitoDTO>>(json);
                                if (pollitos != null)
                                {
                                    PollitoRecibido?.Invoke(pollitos);
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
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        // Metodo para recibir la informacion del servidor
        public void RecibirLista()
        {
            try
            {
                // Creamos un buffer para almacenar los datos recibidos
                byte[] buffer = new byte[tcpClient.Client.ReceiveBufferSize];
                // Obtenemos el stream del cliente
                NetworkStream stream = tcpClient.GetStream();
                // Leemos los datos del stream
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                // Convertimos los bytes a string
                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                // Deserializamos el JSON a una lista de PollitoDTO
                List<PollitoDTO> lista = JsonConvert.DeserializeObject<List<PollitoDTO>>(json);
                // Invocamos la acción con la lista recibida
                PollitoRecibido?.Invoke(lista);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        public string ObtenerIPLocal()
        {
            return tcpClient.Client.RemoteEndPoint.ToString();
        }
    }
}