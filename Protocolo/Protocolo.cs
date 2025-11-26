// ************************************************************************
// Practica 07 – Cliente/Servidor TCP con Protocolo Unificado
// Daniel Carrión / Jannys Garrido
// Fecha de realización: 26/11/2025
// Fecha de entrega: 03/12/2025
//
// Resultados:
// * Se logró integrar una nueva clase Protocolo que centraliza toda la lógica 
//   de procesamiento de mensajes, permitiendo que tanto el cliente como el 
//   servidor utilicen una única estructura para interpretar operaciones, 
//   validar datos y generar respuestas.
// * El servidor fue corregido y reorganizado para delegar completamente la 
//   lógica de negocio a la clase Protocolo, resolviendo errores previos en 
//   la impresión del puerto y en el manejo de solicitudes.
// * Se corrigieron problemas con Git y GitHub relacionados con permisos, 
//   repositorios remotos y ramas inexistentes, logrando establecer un flujo 
//   funcional que incluye creación de ramas, commits, push y gestión mediante 
//   forks, garantizando la correcta subida del proyecto y sus modificaciones.
// * El cliente fue actualizado para generar solicitudes utilizando únicamente 
//   el método HazOperacion(), evitando el armado manual de cadenas y mejorando 
//   la consistencia del protocolo.
// * Se verificó el funcionamiento del contador por cliente, la validación de 
//   placas y la asignación de indicadores de día, confirmando la correcta 
//   comunicación entre cliente y servidor.
//
// Conclusiones:
// Daniel:
// * La práctica permitió comprender de manera más profunda la interacción entre 
//   cliente y servidor mediante sockets TCP, y cómo una clase centralizada 
//   facilita la mantenibilidad y escalabilidad del sistema.
// * El uso de Git y GitHub evidenció la importancia de una correcta gestión de 
//   ramas, permisos y remotos para evitar conflictos y mantener un flujo de 
//   trabajo ordenado.
//
// Jannys:
// * La reorganización del código destacó la importancia de separar responsabilidades, 
//   permitiendo que el servidor se enfoque únicamente en la comunicación y que la 
//   lógica se encuentre unificada en la clase Protocolo.
// * Los problemas encontrados durante la subida del proyecto mostraron lo esencial 
//   que es comprender el funcionamiento de los repositorios, forks y autenticación 
//   para evitar errores como el 403 o refspec inexistentes.
//
// Recomendaciones:
// Daniel:
// * Se recomienda continuar empleando arquitecturas modulares donde la lógica 
//   principal se centralice en clases especializadas, facilitando la reutilización 
//   del código y la corrección de errores.
// * Se sugiere profundizar en el manejo de protocolos personalizados y en el uso 
//   de herramientas de depuración para sockets, ya que permiten analizar con mayor 
//   precisión la comunicación entre procesos distribuídos.
//
// Jannys:
// * Antes de realizar un push, verificar siempre el remoto configurado con 
//   `git remote -v`, ya que esto evita errores de permisos y asegura que los cambios 
//   se envíen al repositorio correcto.
// * Mantener una nomenclatura clara y documentar adecuadamente las clases y métodos, 
//   especialmente en aplicaciones cliente-servidor, mejora la legibilidad y facilita 
//   que otros desarrolladores comprendan el flujo completo del protocolo.
//
// ************************************************************************

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
