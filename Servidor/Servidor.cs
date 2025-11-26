// ============================================================================
// Proyecto: AD_PA01_2024A - Sistema de Validación "Hoy no Circula"
// Módulo: Servidor
// Descripción: Servidor TCP que procesa solicitudes de validación de placas
// Autores: Carrión D., Garrido J.
// Fecha: Noviembre 2025
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo;

namespace Servidor
{
    /// <summary>
    /// Clase principal del servidor que gestiona conexiones de clientes
    /// y procesa solicitudes de validación de placas vehiculares.
    /// </summary>
    class Servidor
    {
        // Objeto para escuchar conexiones entrantes en el puerto 8080
        private static TcpListener escuchador;

        // Diccionario que almacena el número de solicitudes por dirección de cliente
        private static Dictionary<string, int> listadoClientes
            = new Dictionary<string, int>();

        // Instancia del protocolo de comunicación
        private static Protocolo.Protocolo protocolo = new Protocolo.Protocolo();

        /// <summary>
        /// Punto de entrada principal del servidor.
        /// Inicializa el escuchador TCP y acepta conexiones de clientes.
        /// </summary>
        /// <param name="args">Argumentos de línea de comandos (no utilizados)</param>
        static void Main(string[] args)
        {
            try
            {
                // Crea un escuchador en cualquier dirección IP disponible en el puerto 8080
                escuchador = new TcpListener(IPAddress.Any, 8080);
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 8080...");

                // Bucle infinito para aceptar conexiones de clientes
                while (true)
                {
                    // Acepta una conexión de cliente (bloqueante)
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString());

                    // Crea un nuevo hilo para manejar el cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                // Captura errores de socket
                Console.WriteLine("Error de socket al iniciar el servidor: " +
                    ex.Message);
            }
            finally
            {
                // Asegura que el escuchador se detiene al salir
                escuchador?.Stop();
            }
        }

        /// <summary>
        /// Método ejecutado en un hilo separado para manejar la comunicación con un cliente.
        /// </summary>
        /// <param name="obj">Objeto TcpClient del cliente a manejar</param>
        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                // Obtiene el flujo de red para comunicación con el cliente
                flujo = cliente.GetStream();
                byte[] bufferTx;  // Buffer para transmisión
                byte[] bufferRx = new byte[1024];  // Buffer para recepción
                int bytesRx;  // Número de bytes recibidos

                // Bucle para leer múltiples solicitudes del cliente
                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    // Convierte los bytes recibidos a un objeto Pedido
                    Pedido pedido = protocolo.BytesAPedido(bufferRx, bytesRx);
                    Console.WriteLine("Se recibio: " + pedido);

                    // Obtiene la dirección del cliente para seguimiento
                    string direccionCliente =
                        cliente.Client.RemoteEndPoint.ToString();

                    // Resuelve el pedido y obtiene la respuesta
                    Respuesta respuesta = ResolverPedido(pedido, direccionCliente);
                    Console.WriteLine("Se envió: " + respuesta);

                    // Convierte la respuesta a bytes y la transmite
                    bufferTx = protocolo.RespuestaABytes(respuesta);
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }

            }
            catch (SocketException ex)
            {
                // Captura errores de socket durante la comunicación
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message);
            }
            finally
            {
                // Asegura el cierre de recursos
                flujo?.Close();
                cliente?.Close();
            }
        }

        /// <summary>
        /// Resuelve un pedido y retorna la respuesta correspondiente.
        /// Procesa los comandos: INGRESO, CALCULO y CONTADOR.
        /// </summary>
        /// <param name="pedido">Pedido a resolver</param>
        /// <param name="direccionCliente">Dirección IP del cliente para seguimiento</param>
        /// <returns>Objeto Respuesta con el resultado del procesamiento</returns>
        private static Respuesta ResolverPedido(Pedido pedido, string direccionCliente)
        {
            // Respuesta por defecto para comandos no reconocidos
            Respuesta respuesta = protocolo.CrearRespuestaNOK("Comando no reconocido");

            // Procesa el comando recibido
            switch (pedido.Comando)
            {
                // Comando para validar credenciales de ingreso
                case "INGRESO":
                    // Verifica que se proporcionen 2 parámetros: usuario y contraseña
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        // Genera una respuesta aleatoria (50% aceptación)
                        respuesta = new Random().Next(2) == 0
                            ? protocolo.CrearRespuestaOK("ACCESO_CONCEDIDO")
                            : protocolo.CrearRespuestaNOK("ACCESO_NEGADO");
                    }
                    else
                    {
                        // Credenciales inválidas
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                // Comando para calcular el indicador de día de pico y placa
                case "CALCULO":
                    // Verifica que se proporcionen 3 parámetros: modelo, marca y placa
                    if (pedido.Parametros.Length == 3)
                    {
                        string modelo = pedido.Parametros[0];
                        string marca = pedido.Parametros[1];
                        string placa = pedido.Parametros[2];

                        // Valida que la placa tenga el formato correcto
                        if (ValidarPlaca(placa))
                        {
                            // Obtiene el indicador del día de restricción
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = protocolo.CrearRespuestaOK($"{placa} {indicadorDia}");

                            // Incrementa el contador de solicitudes para este cliente
                            ContadorCliente(direccionCliente);
                        }
                        else
                        {
                            // Formato de placa inválido
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                // Comando para obtener el número de solicitudes del cliente
                case "CONTADOR":
                    // Verifica si el cliente tiene solicitudes previas registradas
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = protocolo.CrearRespuestaOK(
                            listadoClientes[direccionCliente].ToString());
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        /// <summary>
        /// Valida que una placa tenga el formato correcto: 3 letras mayúsculas seguidas de 4 dígitos.
        /// Patrón: ^[A-Z]{3}[0-9]{4}$
        /// </summary>
        /// <param name="placa">Placa a validar</param>
        /// <returns>True si la placa es válida, False en caso contrario</returns>
        private static bool ValidarPlaca(string placa)
        {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        /// <summary>
        /// Obtiene el indicador de día de restricción basado en el último dígito de la placa.
        /// Cada dígito corresponde a un día de la semana usando un byte en formato binario.
        /// 
        /// Mapeo:
        /// - Lunes (1, 2): 0b00000001
        /// - Martes (3, 4): 0b00010000
        /// - Miércoles (5, 6): 0b00001000
        /// - Jueves (7, 8): 0b00000100
        /// - Viernes (9, 0): 0b00000010
        /// </summary>
        /// <param name="placa">Placa vehicular en formato XXX####</param>
        /// <returns>Byte con el indicador de día en formato binario</returns>
        private static byte ObtenerIndicadorDia(string placa)
        {
            // Extrae el último dígito de la placa
            int ultimoDigito = int.Parse(placa.Substring(6, 1));

            // Retorna el indicador correspondiente
            switch (ultimoDigito)
            {
                // Lunes: últimos dígitos 1 o 2
                case 1:
                case 2:
                    return 0b00000001; // CORRECCIÓN: Era 0b00100000, ahora es 0b00000001

                // Martes: últimos dígitos 3 o 4
                case 3:
                case 4:
                    return 0b00010000;

                // Miércoles: últimos dígitos 5 o 6
                case 5:
                case 6:
                    return 0b00001000;

                // Jueves: últimos dígitos 7 o 8
                case 7:
                case 8:
                    return 0b00000100;

                // Viernes: últimos dígitos 9 o 0
                case 9:
                case 0:
                    return 0b00000010;

                // Caso por defecto (nunca debería ocurrir)
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Incrementa el contador de solicitudes para un cliente específico.
        /// Si el cliente no existe en el diccionario, lo agrega con contador = 1.
        /// </summary>
        /// <param name="direccionCliente">Dirección IP del cliente</param>
        private static void ContadorCliente(string direccionCliente)
        {
            // Verifica si el cliente ya tiene solicitudes registradas
            if (listadoClientes.ContainsKey(direccionCliente))
            {
                // Incrementa el contador existente
                listadoClientes[direccionCliente]++;
            }
            else
            {
                // Agrega al cliente con contador inicial de 1
                listadoClientes[direccionCliente] = 1;
            }
        }
    }
}
