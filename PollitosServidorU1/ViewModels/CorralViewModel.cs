using CommunityToolkit.Mvvm.ComponentModel;
using PollitosServidorU1.Models;
using PollitosServidorU1.Services;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PollitosServidorU1.ViewModels
{
    public class CorralViewModel : ObservableObject
    {
        #region Propiedades,servicios y Corral
        private readonly string[] Images = new string[2] { "🐥", "🌽" };
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        public int NumMaiz { get; set; } = 5;
        public int TamañoCorral => Columnas * Renglones;
        public Corral Corral { get; set; }
        private static readonly TcpServidor Servidor = new TcpServidor();
        private readonly Random r = new Random(); 
        private readonly Timer Contador;
        #endregion
        public CorralViewModel()
        {
            Corral = new Corral(TamañoCorral);
            //Metodo para Generar una cantidad fija de Maiz
            GenerarMaiz();
            // Suscribir al evento de recepción de pollitos
            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
            Servidor.ClienteDesconectado += Servidor_ClienteDesconectado;
            // Inicializar el temporizador para eliminar pollitos cada 5 segundos
            Contador = new Timer(EliminarMaiz,null, 1000, 1000);
        }

        #region Eventos
        private void Servidor_ClienteDesconectado(string cliente)
        {
            var pollo = Corral.Pollos.FirstOrDefault(x => x != null && x.Cliente == cliente);
            if (pollo != null)
            {
                Corral.Pollos[pollo.Posicion] = null;
                var listaActualizada = Corral.Pollos.Where(x => x != null).ToList();
                Servidor.RetransmitirLista(listaActualizada);
            }
        }
        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //1.- Verificar si la persona esta en el tablero
                var polloEnTablero = Corral.Pollos.
                    FirstOrDefault(x => x != null && x.Nombre == dto.Nombre);
                //Si no esta: agregarlo en la primera posicion disponible
                if (polloEnTablero == null)
                {
                    // Recorrer el corral para encontrar una casilla vacia
                    for (int i = 0; i < Corral.Pollos.Count; i++)
                    {
                        if (Corral.Pollos[i] == null)
                        {
                            //Agregar el pollito al tablero
                            Corral.Pollos[i] = dto;
                            break;
                        }
                    }
                }
                // Si esta: moverlo
                else
                {
                    //Si se esta moviendo no es otro usuario
                    if (dto.Direccion != 0)
                    {
                        // Verificar si el movimiento es válido
                        if (EsMovimientoValido(dto.Posicion, dto.Direccion))
                        {
                            // Mover el pollito
                            MoverPollito(dto.Posicion, dto.Direccion);
                            // Retransmitir la lista de pollitos
                        }
                    }
                    else
                    {
                        Servidor.DesconectarCliente(dto.Cliente);
                    }
                }
                var lista = Corral.Pollos.Where(x => x != null).ToList();

                //Retransmitir la lista si se ha movido o se agrego un nuevo cliente
                //Al retransmitir la lista, tarda un poco en actualizar la lista en el cliente
                Servidor.RetransmitirLista(lista);
            });
        }
        #endregion
        #region Movimiento
        //Verificar si el movimiento es válido
        private bool EsMovimientoValido(int posicion, int direccion)
        {
            switch (direccion)
            {
                case 1:
                    return posicion >= Columnas; // Arriba
                case 2:
                    return posicion < Columnas * (Renglones - 1); // Abajo
                case 3:
                    return posicion % Columnas != 0; // Izquierda
                case 4:
                    return (posicion + 1) % Columnas != 0; // Derecha
                default:
                    return false;
            }
        }
        public void MoverPollito(int posicion, int direccion)
        {
            // Si la posición no está vacía, no hacer nada
            if (Corral.Pollos[posicion] == null) return;

            // Variable para la nueva posición
            int nuevaPosicion = posicion;

            // Asignar la nueva posición dependiendo de la dirección
            switch (direccion)
            {
                // Arriba
                case 1 when nuevaPosicion >= Columnas: nuevaPosicion -= Columnas; break;
                // Abajo
                case 2 when nuevaPosicion < (TamañoCorral - Columnas): nuevaPosicion += Columnas; break;
                // Izquierda
                case 3 when nuevaPosicion % Columnas != 0: nuevaPosicion -= 1; break;
                // Derecha
                case 4 when nuevaPosicion % Columnas != (Columnas - 1): nuevaPosicion += 1; break;
                default: return; // Si la dirección no es válida, salir del método
            }

            // Si no hay nada en la nueva posición, mover el pollito
            if (Corral.Pollos[nuevaPosicion] == null)
            {
                // Mover el pollito
                Corral.Pollos[nuevaPosicion] = Corral.Pollos[posicion];
                // Quitar el pollito de su posición original
                Corral.Pollos[posicion] = null;
                // Cambiar el valor de su posición
                Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
            }
            // Si hay un maíz, "comerlo", aumentar la puntuación y generar un nuevo maíz
            else if (Corral.Pollos[nuevaPosicion].Imagen == "🌽")
            {
                // Asignar la nueva posición
                Corral.Pollos[nuevaPosicion] = Corral.Pollos[posicion];
                // Aumentar la puntuación
                Corral.Pollos[nuevaPosicion].Puntuacion++;
                // Asignar la nueva posición
                Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
                // Eliminar el pollito original
                Corral.Pollos[posicion] = null;
                // Generar un nuevo maíz
                GenerarNuevoMaiz();
            }
        }
        #endregion
        #region Generacion y Eliminacion de Maiz
        // Metodo para manejar la cantidad de maiz generado
        private void GenerarMaiz()
        {
            // Modificar NumMaiz para cambiar la cantidad de maiz generado en pantalla
            for (int i = 0; i < NumMaiz; i++)
            {
                //Metodo para generar un nuevo maíz
                GenerarNuevoMaiz();
            }
        }
        //Metodo para generar un nuevo maiz
        private void GenerarNuevoMaiz()
        {
            int nuevaPosicion;
            do
            {
                nuevaPosicion = r.Next(0, TamañoCorral);
            }
            while (Corral.Pollos[nuevaPosicion] != null);

            Corral.Pollos[nuevaPosicion] = new PollitoDTO
            {
                Puntuacion = -10,
                Imagen = Images[1],
                Posicion = nuevaPosicion
            };
        }
        //no es necesario recibir nada, pero el objeto state es necesario para la instanciacion del timer
        private void EliminarMaiz(object state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Lista de Maiz
                var lista = Corral.Pollos.Where(x => x != null && x.Imagen == Images[1]).ToList();
                //Recorrer la lista de Maiz
                foreach (var maiz in lista)
                {
                    //Si la duracion es 0
                    if (maiz.Duracion == 0)
                    {
                        //Eliminar Maiz
                        Corral.Pollos[maiz.Posicion] = null;
                        //Generar uno nuevo
                        GenerarNuevoMaiz();
                    }
                    else
                    {
                        //Reducir la Duracion del maiz en 1
                        maiz.Duracion--;
                    }
                }

                var listaActualizada = Corral.Pollos.Where(x => x != null).ToList();
                Servidor.RetransmitirLista(listaActualizada);
            });
        }
        #endregion
    }
}
