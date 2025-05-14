using CommunityToolkit.Mvvm.ComponentModel;
using PollitosServidorU1.Models;
using PollitosServidorU1.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PollitosServidorU1.ViewModels
{
    public class CorralViewModel : ObservableObject
    {
        private readonly string[] Images = new string[2] { "🐥", "🌽" };
        public int Columnas { get; set; } = 10;
        public int Renglones { get; set; } = 10;
        public int NumMaiz { get; set; } = 5;
        public int TamañoCorral => Columnas * Renglones ;
        public Corral Corral { get; set; }
        private static readonly TcpServidor Servidor = new TcpServidor();
        private readonly Random r = new Random();
        private readonly Timer Contador;
        public CorralViewModel()
        {
            Corral = new Corral(TamañoCorral);
            GenerarMaiz();
            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
            Servidor.ClienteDesconectado += Servidor_ClienteDesconectado;
            Contador = new Timer(EliminarMaiz, null, 1000, 1000);
        }
        private void Servidor_ClienteDesconectado(string cliente)
        {
            var pollo = Corral.Pollos.FirstOrDefault(x => x != null && x.Cliente == cliente);
            if (pollo != null)
            {
                Corral.Pollos[pollo.Posicion] = null;
                // Retransmitimos el pollito con la imagen vacia para que se elimine del cliente
                Servidor.Retransmitir(new PollitoDTO
                {
                    // posicion del pollito a eliminar
                    Posicion = pollo.Posicion,
                    Imagen = null // indica eliminación
                });
            }
        }
        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                var polloEnTablero = Corral.Pollos.FirstOrDefault(x => x != null && x.Nombre == dto.Nombre);
                // Si no existe el pollito en el tablero, lo añadimos
                if (polloEnTablero == null)
                {
                    ManejarNuevoPollito(dto);
                }
                // Si existe y es del mismo cliente, lo movemos
                else if (dto.Direccion > 0 && dto.Direccion < 5 && EsMovimientoValido(polloEnTablero.Posicion, dto.Direccion))
                {
                    MoverPollito(polloEnTablero.Posicion, dto.Direccion);
                }
            }));
        }
        private void ManejarNuevoPollito(PollitoDTO dto)
        {
            // Buscamos si ya existe un pollito con el mismo nombre
            var polloExistente = Corral.Pollos.FirstOrDefault(x => x != null && x.Nombre == dto.Nombre);

            // Si existe y es del mismo cliente, lo eliminamos, esto no deberia pasar 
            // pero es una validacion adicional por si acaso
            if (polloExistente != null && polloExistente.Cliente != dto.Cliente)
            {
                Servidor.DesconectarCliente(dto.Cliente);
                return;
            }                                                                                               

            // Si no existe, lo añadimos al corral
            for (int i = 0; i < Corral.Pollos.Count; i++)
            {
                // Buscamos un espacio vacío
                if (Corral.Pollos[i] == null)
                {
                    // Asignamos la posición 
                    dto.Posicion = i;
                    // Agregamos el pollito al corral
                    Corral.Pollos[i] = dto;
                    // Retransmitimos el pollito a todos los clientes
                    Servidor.Retransmitir(dto);
                   
                    EnviarTablero();
                    // interrumpimos el ciclo
                    break;
                }
            }
        }
        private void EnviarTablero()
        {
            var tablero = Corral.Pollos.Where(x => x != null).ToList();
            //  Retransmitimos todo el tablero
            foreach (var item in tablero)
            {
                Servidor.Retransmitir(item);
            }
        }
        private bool EsMovimientoValido(int posicion, int direccion)
        {
            int nuevaPosicion = CalcularNuevaPosicion(posicion, direccion);
            if (nuevaPosicion < 0 || nuevaPosicion >= TamañoCorral) return false;

            var destino = Corral.Pollos[nuevaPosicion];
            return destino == null || destino.Imagen == Images[1]; // Espacio vacío o maíz
        }
        private int CalcularNuevaPosicion(int posicion, int direccion)
        {
            switch (direccion)
            {
                case 1:
                    return posicion >= Columnas ? posicion - Columnas : posicion; // Arriba
                case 2:
                    return posicion < (TamañoCorral - Columnas) ? posicion + Columnas : posicion; // Abajo
                case 3:
                    return posicion % Columnas != 0 ? posicion - 1 : posicion; // Izquierda
                case 4:
                    return posicion % Columnas != (Columnas - 1) ? posicion + 1 : posicion; // Derecha
                default:
                    return posicion;
            }
        }
        public void MoverPollito(int posicion, int direccion)
        {
            // Validar si el pollito existe en la posición actual
            if (Corral.Pollos[posicion] == null) return;
            // Validar si el movimiento es válido
            int nuevaPosicion = CalcularNuevaPosicion(posicion, direccion);
            // Validar si la nueva posición es diferente
            if (nuevaPosicion == posicion) return;
            // Validar si la nueva posición es válida
            if (nuevaPosicion < 0 || nuevaPosicion >= TamañoCorral) return;
            // Validar si la nueva posición está ocupada por otro pollito
            if (Corral.Pollos[nuevaPosicion] != null && Corral.Pollos[nuevaPosicion].Imagen != Images[1]) return;
            // Mover el pollito
            var pollito = Corral.Pollos[posicion];
            // Validar si el pollito comió maíz
            bool comioMaiz = Corral.Pollos[nuevaPosicion]?.Imagen == Images[1];
            if (comioMaiz)
            {
                // Aumentar la puntuación del pollito
                pollito.Puntuacion++;
                // Generar nuevo maíz tras comer
                GenerarNuevoMaiz(); 
            }
            // Agregar el pollito a la nueva posición
            Corral.Pollos[nuevaPosicion] = pollito;     
            // Limpiar la posición anterior
            Corral.Pollos[posicion] = null;
            // Asignar la nueva posición y dirección   
            Corral.Pollos[nuevaPosicion].Posicion = nuevaPosicion;
            Corral.Pollos[nuevaPosicion].Direccion = direccion;
           

            // Enviar solo el que se movio
            Servidor.Retransmitir(pollito);
            // Enviar un pollito con la posicion antigua y la imagen vacia 
            //para que se elimine del cliente
            Servidor.Retransmitir(new PollitoDTO { Posicion = posicion, Imagen = null });
        }
        private void GenerarMaiz()
        {
            for (int i = 0; i < NumMaiz; i++)
            {
                GenerarNuevoMaiz();
            }
        }
        private void GenerarNuevoMaiz()
        {
            // buscar una nueva posicion
            int nuevaPos;
            do
            {
                nuevaPos = r.Next(0, TamañoCorral);
            }
            //mientras la nueva posicion no sea null
            while (Corral.Pollos[nuevaPos] != null);
            // crear un maiz en dicha posicion
            var maiz = new PollitoDTO
            {
                Imagen = Images[1],
                Posicion = nuevaPos,
                Puntuacion = -10,//ocultar puntuacion en cliente
                Duracion = 5 // duracion del maiz en pantalla
            };
            //colocar en tablero
            Corral.Pollos[nuevaPos] = maiz;
            //enviar al cliente
            Servidor.Retransmitir(maiz);

        }
        private void EliminarMaiz(object state)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                // Obtener los maices
                var maices = Corral.Pollos.Where(x => x != null && x.Imagen == Images[1]).ToList();
                                     
                // Disminuir su durabilidad en 1
                foreach (var maiz in maices)
                {
                    maiz.Duracion--;
                }
                // Obtener maices expirados
                var expirados = maices.Where(m => m.Duracion <= 0).ToList();
                // Recorrer maices expirados
                foreach (var maiz in expirados)
                {
                    // Eliminar maiz expirado del tablero
                    Corral.Pollos[maiz.Posicion] = null;
                  
                    //enviar al cliente un nuevo maiz con la posicion del maiz expirado y su posicion
                    var eliminarPollito = new PollitoDTO
                    {
                        Posicion = maiz.Posicion,
                        Imagen = null // indica que se debe borrar del cliente
                    };

                    //retransmitir la posicion del maiz a eliminar
                    Servidor.Retransmitir(eliminarPollito);
                    // Generar nuevo maiz
                    GenerarNuevoMaiz();
                }
            }));
        }
    }
}
