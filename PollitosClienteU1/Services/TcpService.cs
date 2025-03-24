using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PollitosClienteU1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PollitosClienteU1.Services
{
    public class TcpService
    {
        private readonly List<TcpClient> tcpClients = new List<TcpClient>();
        public Action<PollitoDTO> PollitoRecibido;
        public Action<List<PollitoDTO>> MaizRecibido;

        #region Metodos de conexion
        public void Conectar(string IP)
        {
            try
            {
                var client = new TcpClient();
                client.Connect(IP, 5000);
                tcpClients.Add(client);
                Task.Run(() => Escuchar(client));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Desconectar()
        {
            foreach (var client in tcpClients)
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
            tcpClients.Clear();
        }

        public bool IsConnected()
        {
            return tcpClients.Exists(client => client.Connected);
        }

        public string ObtenerIP(TcpClient client)
        {
            return client.Client.LocalEndPoint.ToString();
        }
        #endregion

        public void EnviarPollito(PollitoDTO dto)
        {
            try
            {
                foreach (var client in tcpClients)
                {
                    if (dto.Cliente == null)
                    {
                        dto.Cliente = ObtenerIP(client);
                    }
                    var json = JsonConvert.SerializeObject(dto);
                    var buffer = Encoding.UTF8.GetBytes(json);
                    client.GetStream().Write(buffer, 0, buffer.Length);
                }
            }
            catch (SocketException)
            {
                Desconectar();
            }
        }

        private async void Escuchar(TcpClient client)
        {
            if (client == null) return;
            var stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        await RecibirPollitosAsync(client);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task RecibirPollitosAsync(TcpClient client)
        {
            try
            {
                byte[] buffer = new byte[client.Client.ReceiveBufferSize];
                NetworkStream stream = client.GetStream();
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcesarJson(json);
            }
            catch (SocketException ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ProcesarJson(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token.Type == JTokenType.Array)
                {
                    var maiz = token.ToObject<List<PollitoDTO>>();
                    MaizRecibido?.Invoke(maiz);
                }
                else if (token.Type == JTokenType.Object)
                {
                    var pollito = token.ToObject<PollitoDTO>();
                    PollitoRecibido?.Invoke(pollito);
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Error al procesar JSON: {ex.Message}");
            }
        }

    }
}