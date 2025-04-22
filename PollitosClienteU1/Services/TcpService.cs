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
        private TcpClient tcpClient = new TcpClient();
        public Action<PollitoDTO> PollitoRecibido;

        #region Metodos de conexion
        public void Conectar(string IP)
        {
            try
            {
                if(tcpClient == null)
                {
                    tcpClient = new TcpClient();
                }
                tcpClient.Connect(IP, 5000);
                new Thread(Escuchar) { IsBackground = true }.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void Desconectar()
        {
            if (tcpClient.Connected)
            {
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = null;
            }
        }
        public bool IsConnected()
        {
            return tcpClient.Connected;
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
                if (dto.Cliente == null)
                {
                    dto.Cliente = ObtenerIP(tcpClient);
                }
                var json = JsonConvert.SerializeObject(dto);
                var buffer = Encoding.UTF8.GetBytes(json);
                tcpClient.GetStream().Write(buffer, 0, buffer.Length);

            }
            catch (SocketException)
            {
                Desconectar();
            }
        }
        private async void Escuchar()
        {
            if (tcpClient == null) return;
            var stream = tcpClient.GetStream();
            try
            {
                while (tcpClient.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        await RecibirPollitosAsync(tcpClient);
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
                ///<summary>
                ///   se reciben objetos y no un arreglo de objeto como tal, asi que convertimos el json recibido en una lista de objetos
                /// </summary>

                json = "[" + json.Replace("}{", "},{") + "]";
                // Verificar si el JSON es nulo o vacío
                var token = JToken.Parse(json);
                //Verificar si es un Array o un Object
                if (token.Type == JTokenType.Array)
                {
                    // Si es un array, lo convertimos a una lista de PollitoDTO
                    var ListaMaiz = token.ToObject<List<PollitoDTO>>();
                    foreach (var maiz in ListaMaiz)
                    {
                        PollitoRecibido?.Invoke(maiz);
                    }
                }
                // Si es un objeto, lo convertimos a PollitoDTO
                else if (token.Type == JTokenType.Object)
                {
                    var pollito = token.ToObject<PollitoDTO>();
                    // Si el pollito no es nulo, lo invocamos
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