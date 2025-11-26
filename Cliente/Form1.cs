// ************************************************************************
// Practica 07 – Cliente/Servidor TCP con Protocolo Unificado
// Jannys Garrido
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
// * La reorganización del código destacó la importancia de separar responsabilidades, 
//   permitiendo que el servidor se enfoque únicamente en la comunicación y que la 
//   lógica se encuentre unificada en la clase Protocolo.
// * Los problemas encontrados durante la subida del proyecto mostraron lo esencial 
//   que es comprender el funcionamiento de los repositorios, forks y autenticación 
//   para evitar errores como el 403 o refspec inexistentes.
//
// Recomendaciones:

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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Protocolo;

namespace Cliente
{
    /// <summary>
    /// Formulario principal de la aplicación cliente para el sistema "Hoy no Circula".
    /// Permite a los usuarios autenticarse y consultar restricciones de pico y placa.
    /// </summary>
    public partial class FrmValidador : Form
    {
        // Cliente TCP para conexión con el servidor
        private TcpClient remoto;

        // Flujo de red para transmisión de datos
        private NetworkStream flujo;

        // Instancia del protocolo de comunicación
        private Protocolo.Protocolo protocolo = new Protocolo.Protocolo();

        /// <summary>
        /// Constructor del formulario.
        /// </summary>
        public FrmValidador()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Evento que se ejecuta cuando el formulario se carga.
        /// Establece la conexión inicial con el servidor.
        /// </summary>
        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Intenta conectar con el servidor en localhost:8080
                remoto = new TcpClient("127.0.0.1", 8080);
                flujo = remoto.GetStream();
            }
            catch (SocketException ex)
            {
                // Muestra mensaje de error si falla la conexión
                MessageBox.Show("No se puedo establecer conexión " + ex.Message,
                    "ERROR");
            }

            // Desactiva los controles de placa hasta que el usuario inicie sesión
            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        /// <summary>
        /// Evento del botón "Iniciar" para autenticar al usuario.
        /// </summary>
        private void btnIniciar_Click(object sender, EventArgs e)
        {
            // Obtiene usuario y contraseña del formulario
            string usuario = (txtUsuario.Text ?? "").Trim();
            string contraseña = (txtPassword.Text ?? "").Trim();

            // Valida que ambos campos estén completados
            if (usuario == "" || contraseña == "")
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña",
                    "ADVERTENCIA");
                return;
            }

            // Crea un pedido de ingreso con las credenciales
            Pedido pedido = protocolo.CrearPedido("INGRESO", usuario, contraseña);

            // Envía el pedido y recibe la respuesta
            Respuesta respuesta = HazOperacion(pedido);

            // Verifica si hubo error en la transmisión
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error en la comunicación", "ERROR");
                return;
            }

            // Valida si el acceso fue concedido
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                // Habilita los controles de placa y desactiva el panel de login
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                txtModelo.Focus();
            }
            // Valida si el acceso fue negado
            else if (respuesta.Estado == "NOK" && respuesta.Mensaje == "ACCESO_NEGADO")
            {
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("No se pudo ingresar, revise credenciales",
                    "ERROR");
                txtUsuario.Focus();
            }
        }

        /// <summary>
        /// Transmite un pedido al servidor y recibe la respuesta.
        /// Maneja la serialización/deserialización usando el protocolo.
        /// </summary>
        /// <param name="pedido">Pedido a enviar</param>
        /// <returns>Respuesta del servidor o null si hay error</returns>
        private Respuesta HazOperacion(Pedido pedido)
        {
            // Verifica si hay conexión disponible
            if (flujo == null)
            {
                MessageBox.Show("No hay conexión", "ERROR");
                return null;
            }

            try
            {
                // Convierte el pedido a bytes usando el protocolo
                byte[] bufferTx = protocolo.PedidoABytes(pedido);

                // Transmite el pedido al servidor
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // Buffer para recibir la respuesta
                byte[] bufferRx = new byte[1024];

                // Lee la respuesta del servidor
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);

                // Convierte los bytes recibidos a objeto Respuesta usando el protocolo
                Respuesta respuesta = protocolo.BytesARespuesta(bufferRx, bytesRx);

                return respuesta;
            }
            catch (SocketException ex)
            {
                // Muestra error si hay problema en la transmisión
                MessageBox.Show("Error al intentar transmitir " + ex.Message,
                    "ERROR");
            }

            return null;
        }

        /// <summary>
        /// Evento del botón "Consultar" para validar una placa.
        /// </summary>
        private void btnConsultar_Click(object sender, EventArgs e)
        {
            // Obtiene los datos de la placa del formulario
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;

            // Crea un pedido de cálculo con los datos de la placa
            Pedido pedido = protocolo.CrearPedido("CALCULO", modelo, marca, placa);

            // Envía el pedido y recibe la respuesta
            Respuesta respuesta = HazOperacion(pedido);

            // Verifica si hubo error en la transmisión
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Verifica si la solicitud fue exitosa
            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
                // Desactiva todos los checkboxes en caso de error
                chkLunes.Checked = false;
                chkMartes.Checked = false;
                chkMiercoles.Checked = false;
                chkJueves.Checked = false;
                chkViernes.Checked = false;
            }
            else
            {
                // Extrae el indicador de día de la respuesta
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("Se recibió: " + respuesta.Mensaje,
                    "INFORMACIÓN");

                // Convierte el segundo elemento a byte (indicador binario)
                byte resultado = Byte.Parse(partes[1]);

                // Según el indicador recibido, marca el checkbox correspondiente
                switch (resultado)
                {
                    // Lunes: 0b00000001
                    case 0b00000001:
                        chkLunes.Checked = true;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;

                    // Martes: 0b00010000
                    case 0b00010000:
                        chkMartes.Checked = true;
                        chkLunes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;

                    // Miércoles: 0b00001000
                    case 0b00001000:
                        chkMiercoles.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;

                    // Jueves: 0b00000100
                    case 0b00000100:
                        chkJueves.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkViernes.Checked = false;
                        break;

                    // Viernes: 0b00000010
                    case 0b00000010:
                        chkViernes.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        break;

                    // Valor no reconocido: desactiva todos los checkboxes
                    default:
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Evento del botón "Número de Consultas" para obtener el contador de solicitudes.
        /// </summary>
        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            // Crea un pedido de contador (sin parámetros significativos)
            Pedido pedido = protocolo.CrearPedido("CONTADOR");

            // Envía el pedido y recibe la respuesta
            Respuesta respuesta = HazOperacion(pedido);

            // Verifica si hubo error en la transmisión
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Verifica si la solicitud fue exitosa
            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
            }
            else
            {
                // Extrae el número de consultas del mensaje de respuesta
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("El número de pedidos recibidos en este cliente es " + partes[0],
                    "INFORMACIÓN");
            }
        }

        /// <summary>
        /// Evento que se ejecuta cuando se cierra el formulario.
        /// Asegura que los recursos de red se liberen correctamente.
        /// </summary>
        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cierra el flujo de red si existe
            if (flujo != null)
                flujo.Close();

            // Cierra la conexión TCP si existe y está conectada
            if (remoto != null)
                if (remoto.Connected)
                    remoto.Close();
        }
    }
}
