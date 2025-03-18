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
        public Action<List<PollitoDTO>> ListaRecibida;
        public TcpService()
        {
            tcpClient = new TcpClient();
        }

        #region Metodos de conexion
        //Este metodo se encarga de conectar el cliente con el servidor
        public void Conectar(string IP)
        {
            //Intenta conectar el cliente al servidor
            try
            {
                tcpClient.Connect(IP, 5000);
            }
            catch (Exception ex)
            {
                // Se mostrara un mensaje en caso de que no haya conexion con el servidor
                MessageBox.Show(ex.Message);
            }
        }

        //Este metodo se encarga de desconectar el cliente del servidor
        public void Desconectar()
        {
            //si el cliente no esta conectado
            if (!tcpClient.Connected)
            {
                // Cerramos la conexion limpiamente
                tcpClient.Close();
            }
        }

        // Este metodo sirve para verificar si el cliente esta conectado
        public bool IsConnected()
        {
            // Regresa un true si el cliente esta conectado,
            // En caso contrario regresara un false
            return tcpClient.Connected;
        }
        #endregion

        // Metodo para enviar un polllito al servidor
        public void EnviarPollito(PollitoDTO dto)
        {
            try
            {
                //Si el cliente del pollito es nulo
                if (dto.Cliente == null)
                {
                    // Asignamos la IP del cliente al objeto PollitoDTO
                    dto.Cliente = ObtenerIP();
                }
                // Serializamos el objeto a JSON
                var json = JsonConvert.SerializeObject(dto);
                // Convertimos el JSON a bytes
                var buffer = Encoding.UTF8.GetBytes(json);
                // Enviamos los bytes al servidor
                tcpClient.GetStream().Write(buffer, 0, buffer.Length);
                // Creamos un hilo para escuchar la respuesta del servidor
                Thread hilo = new Thread(Escuchar) { IsBackground = true };
                // Iniciamos el hilo
                hilo.Start();
            }
            catch (SocketException)
            {
                //Si ocurre un error desconectamos el cliente
                Desconectar();
            }
        }

        // Metodo para escuchar cambios en el servidor 
        private void Escuchar()
        {
            // Si el cliente no es nulo
            if (tcpClient != null)
            {
                // Asignamos tcpClient como client
                var client = tcpClient;
                // Obtenemos el flujo de los datos
                var stream = client.GetStream();
                try
                {
                    // Mientras el cliente este conectado
                    while (client.Connected)
                    {
                        // Si tiene informacion disponible
                        if (stream.DataAvailable)
                        {
                            RecibirLista();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Se puede mostrar un messagebox en su lugar, por cuestiones de
                    //simpleza utilizo el Debug.WriteLine
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
                try
                {
                    // Deserializamos el JSON a un objeto PollitoDTO  
                    var pollitos = JsonConvert.DeserializeObject<List<PollitoDTO>>(json);
                    if (pollitos != null)
                    {
                        // Invocamos el evento ListaRecibida y se le da la lista de pollitos
                        ListaRecibida?.Invoke(pollitos);
                    }
                }
                catch (JsonException ex)
                {
                    //Se puede mostrar un messagebox en su lugar, por cuestiones de
                    //simpleza utilizo el Debug.WriteLine
                    Debug.WriteLine($"Error de deserialización: {ex.Message}");
                }

            }
            catch (SocketException ex)
            {
                //Se puede mostrar un messagebox en su lugar, por cuestiones de
                //simpleza utilizo el Debug.WriteLine
                Debug.WriteLine(ex.Message);
            }
        }
        //Metodo para obtener la IP local
        public string ObtenerIP()
        {
            //El cliente utiliza la ip local porque la IP remota es la del servidor 
            return tcpClient.Client.LocalEndPoint.ToString();
        }
    }
}