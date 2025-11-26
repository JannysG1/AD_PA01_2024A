// ============================================================================
// Proyecto: AD_PA01_2024A - Sistema de Validación "Hoy no Circula"
// Módulo: Protocolo
// Descripción: Define las clases de comunicación entre cliente y servidor
// Autores: Carrión D., Garrido J.
// Fecha: Noviembre 2025
// ============================================================================

using System;
using System.Linq;

namespace Protocolo
{
    /// <summary>
    /// Clase que representa una solicitud desde el cliente al servidor.
    /// Contiene el comando a ejecutar y sus parámetros asociados.
    /// </summary>
    public class Pedido
    {
        // Comando a ejecutar en el servidor
        public string Comando { get; set; }

        // Parámetros asociados al comando
        public string[] Parametros { get; set; }

        /// <summary>
        /// Procesa un mensaje de texto y lo convierte en un objeto Pedido.
        /// El formato esperado es: "COMANDO param1 param2 ..."
        /// </summary>
        /// <param name="mensaje">Mensaje recibido en formato texto</param>
        /// <returns>Objeto Pedido con comando y parámetros extraídos</returns>
        public static Pedido Procesar(string mensaje)
        {
            // Divide el mensaje por espacios
            var partes = mensaje.Split(' ');
            
            return new Pedido
            {
                // El primer elemento es el comando, convertido a mayúsculas
                Comando = partes[0].ToUpper(),
                // El resto son parámetros
                Parametros = partes.Skip(1).ToArray()
            };
        }

        /// <summary>
        /// Convierte el objeto Pedido a su representación en texto.
        /// </summary>
        /// <returns>String con el comando y parámetros separados por espacios</returns>
        public override string ToString()
        {
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    /// <summary>
    /// Clase que representa la respuesta del servidor al cliente.
    /// Contiene el estado de la operación y un mensaje asociado.
    /// </summary>
    public class Respuesta
    {
        // Estado de la operación: "OK" o "NOK"
        public string Estado { get; set; }

        // Mensaje descriptivo de la respuesta
        public string Mensaje { get; set; }

        /// <summary>
        /// Convierte el objeto Respuesta a su representación en texto.
        /// </summary>
        /// <returns>String con el estado y mensaje separados por espacio</returns>
        public override string ToString()
        {
            return $"{Estado} {Mensaje}";
        }
    }

    /// <summary>
    /// Clase encargada de gestionar el protocolo de comunicación entre cliente y servidor.
    /// Integra el procesamiento de pedidos y respuestas, proporcionando una interfaz
    /// unificada para la comunicación.
    /// </summary>
    public class Protocolo
    {
        /// <summary>
        /// Crea un pedido a partir del comando y parámetros especificados.
        /// </summary>
        /// <param name="comando">Comando a ejecutar</param>
        /// <param name="parametros">Parámetros del comando</param>
        /// <returns>Objeto Pedido construido</returns>
        public Pedido CrearPedido(string comando, params string[] parametros)
        {
            return new Pedido
            {
                Comando = comando.ToUpper(),
                Parametros = parametros
            };
        }

        /// <summary>
        /// Convierte un Pedido a su representación en bytes para transmisión.
        /// </summary>
        /// <param name="pedido">Pedido a convertir</param>
        /// <returns>Array de bytes del pedido en UTF-8</returns>
        public byte[] PedidoABytes(Pedido pedido)
        {
            return System.Text.Encoding.UTF8.GetBytes(pedido.ToString());
        }

        /// <summary>
        /// Convierte una Respuesta a su representación en bytes para transmisión.
        /// </summary>
        /// <param name="respuesta">Respuesta a convertir</param>
        /// <returns>Array de bytes de la respuesta en UTF-8</returns>
        public byte[] RespuestaABytes(Respuesta respuesta)
        {
            return System.Text.Encoding.UTF8.GetBytes(respuesta.ToString());
        }

        /// <summary>
        /// Convierte un array de bytes a un objeto Pedido.
        /// </summary>
        /// <param name="buffer">Buffer de bytes recibido</param>
        /// <param name="longitud">Longitud válida del buffer</param>
        /// <returns>Objeto Pedido decodificado</returns>
        public Pedido BytesAPedido(byte[] buffer, int longitud)
        {
            string mensaje = System.Text.Encoding.UTF8.GetString(buffer, 0, longitud);
            return Pedido.Procesar(mensaje);
        }

        /// <summary>
        /// Convierte un array de bytes a un objeto Respuesta.
        /// </summary>
        /// <param name="buffer">Buffer de bytes recibido</param>
        /// <param name="longitud">Longitud válida del buffer</param>
        /// <returns>Objeto Respuesta decodificado</returns>
        public Respuesta BytesARespuesta(byte[] buffer, int longitud)
        {
            string mensaje = System.Text.Encoding.UTF8.GetString(buffer, 0, longitud);
            var partes = mensaje.Split(' ');
            
            return new Respuesta
            {
                Estado = partes[0],
                Mensaje = string.Join(" ", partes.Skip(1).ToArray())
            };
        }

        /// <summary>
        /// Crea una respuesta satisfactoria.
        /// </summary>
        /// <param name="mensaje">Mensaje de la respuesta</param>
        /// <returns>Objeto Respuesta con estado OK</returns>
        public Respuesta CrearRespuestaOK(string mensaje)
        {
            return new Respuesta { Estado = "OK", Mensaje = mensaje };
        }

        /// <summary>
        /// Crea una respuesta de error.
        /// </summary>
        /// <param name="mensaje">Mensaje de error</param>
        /// <returns>Objeto Respuesta con estado NOK</returns>
        public Respuesta CrearRespuestaNOK(string mensaje)
        {
            return new Respuesta { Estado = "NOK", Mensaje = mensaje };
        }
    }
}