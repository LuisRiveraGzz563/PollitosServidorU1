using PollitosServidorU1.Models;
using PollitosServidorU1.Services;
using System;
using System.Linq;

namespace PollitosServidorU1.ViewModels
{
    class CorralViewModel
    {
        private readonly string[] Images = new string[2] { "🐥", "🌽" };

        public int Columnas = 10, Renglones = 10 ,NumMaiz = 5;
        public int TamañoCorral => Columnas * Renglones;
        private readonly Corral Corral;
        private static readonly TcpService Servidor = new TcpService();
        Random r = new Random();

        public CorralViewModel()
        {
            Corral = new Corral(TamañoCorral);
            GenerarMaiz();
            // Suscribir al evento de recepción de pollitos
            Servidor.PollitoRecibido += Servidor_PollitoRecibido;
        }

        private void Servidor_PollitoRecibido(PollitoDTO dto)
        {
            //1.- Verificar si la persona esta en el tablero
            var polloEnTablero = Corral.Pollos.FirstOrDefault(x => x != null && x.Cliente == dto.Cliente);
            //Si no esta: agregarlo en la primera posicion disponible
            if (polloEnTablero == null)
            {
                //Si la posicion esta vacia
                if (Corral.Pollos[dto.Posicion] == null)
                {
                    //Agregar el pollito al tablero
                    Corral.Pollos[dto.Posicion] = dto;
                }
                else
                {
                    // Recorrer el corral para encontrar una casilla vacia
                    for (int i = 0; i < Corral.Pollos.Count; i++)
                    {
                        //Si la casilla esta vacia
                        if (Corral.Pollos[i] == null)
                        {
                            //Asignar la posicion
                            dto.Posicion = (byte)i;
                            //Salir del ciclo
                            break;
                        }
                    }

                    //retransmitir la lista de pollitos
                    Servidor.Retransmitir(Corral.Pollos.ToList());
                }
            }
            // Si esta: moverlo
            else
            {
                // Verificar si el movimiento es válido
                if (EsMovimientoValido(dto.Posicion, dto.Direccion))
                {
                    // Mover el pollito
                    MoverPollito(dto.Posicion, dto.Direccion);
                    // Retransmitir la lista de pollitos
                    Servidor.Retransmitir(Corral.Pollos.ToList());
                }
                // Si el movimiento no es válido, retransmitir la lista de pollitos
                else
                {
                    Servidor.Retransmitir(Corral.Pollos.ToList());
                }
            }
        }

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
            if (Corral.Pollos[posicion] != null) return;
            // variable para la nueva posición
            int nuevaPosicion = posicion;
            //Asignar la nueva posicion dependiendo de la direccion
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
            }

            // Verificar si hay maíz en la nueva posición
            if (Corral.Pollos[nuevaPosicion] != null && Corral.Pollos[nuevaPosicion].Puntuacion == -10)
            {
                //Aumentar la puntuación
                Corral.Pollos[posicion].Puntuacion++;
                // Eliminar el maíz
                Corral.Pollos[nuevaPosicion] = Corral.Pollito;
                // Asignar la nueva posición
                Corral.Pollito.Posicion = nuevaPosicion;
                // Generar un nuevo maíz
                GenerarNuevoMaiz();
            }

            // Si la nueva posición está vacía, mover
            else if (Corral.Pollos[nuevaPosicion] == null)
            {
                Corral.Pollos[posicion] = null;
                Corral.Pollos[nuevaPosicion] = Corral.Pollito;
                Corral.Pollito.Posicion = nuevaPosicion;
            }
        }
        // Metodo para manejar la cantidad de maiz generado
        private void GenerarMaiz()
        {
            // Modificar NumMaiz para cambiar la cantidad de maiz generado en pantalla
            for (int i = 0; i < NumMaiz; i++)
            {
                // Generar un nuevo maíz
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
                Imagen = Images[1],
                Puntuacion = -10,
                Posicion = nuevaPosicion
            };
        }
    }
}
