using System;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Progama
{
    public class Programa
    {
        private static string? dbFilePath;
        private static int contadorFilasInsertadas = 0; // Contador de filas insertadas

        static void Main(string[] arg)
        {
            // Obtener la ruta de AppData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Definir la carpeta 'DBusuarios' dentro de AppData
            string appFolderPath = Path.Combine(appDataPath, "DBusuarios");

            // Crear la carpeta 'DBusuarios' si no existe
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            // Definir la ruta completa de la base de datos dentro de 'DBusuarios'
            dbFilePath = Path.Combine(appFolderPath, "usuarios.db");

            // Crear la base de datos y la tabla si no existen
            CrearBaseDeDatosYTabla();

            // Inicializar el servidor HTTP
            HttpListener listener = new();
            listener.Prefixes.Add("http://localhost:8080/");

            try
            {
                listener.Start();
                Console.WriteLine("Servidor iniciado en http://localhost:8080/. Presiona cualquier tecla para detenerlo...");

                while (true)
                {
                    // Espera una solicitud
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    try
                    {
                        // Comprobar si es una solicitud POST para /submit
                        if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/submit")
                        {
                            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                string formData = reader.ReadToEnd();
                                Console.WriteLine($"Datos recibidos: {formData}");

                                // Guardar los datos en la base de datos
                                GuardarEnBaseDeDatos(formData);
                            }

                            // Responder al usuario con un mensaje y redirigir a la página principal después de 5 segundos
                            string responseMessage = @"
                                <html>
                                    <body>
                                        <h1>Datos recibidos</h1>
                                        <p>Gracias por registrarte. Hemos recibido tus datos correctamente.</p>
                                        <p>6 segundos de tiempo</p>
                                        <script>
                                            setTimeout(function() {
                                                window.location.href = '/';
                                            }, 8000);
                                        </script>
                                    </body>
                                </html>";
                            byte[] buffer = Encoding.UTF8.GetBytes(responseMessage);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                        }
                        else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/users")
                        {
                            // Obtener y mostrar todos los datos de los usuarios
                            string usuariosHtml = ObtenerUsuariosHTML();
                            byte[] buffer = Encoding.UTF8.GetBytes(usuariosHtml);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                        }
                        else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/")
                        {
                            // Código para manejar la ruta raíz...
                            string htmlPage = File.ReadAllText("C:\\server\\Program\\Progama\\bin\\Debug\\net8.0\\index.html"); // Asegúrate de cambiar la ruta aquí
                            byte[] buffer = Encoding.UTF8.GetBytes(htmlPage);
                            context.Response.ContentLength64 = buffer.Length;
                            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                            context.Response.OutputStream.Close();
                        }
                        else
                        {
                            // Manejo de errores 404...
                            context.Response.StatusCode = 404;
                            context.Response.OutputStream.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al procesar la solicitud: {ex.Message}");
                        context.Response.StatusCode = 500; // Error interno del servidor
                        context.Response.OutputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar el servidor: {ex.Message}");
            }
            finally
            {
                listener.Stop();
                Console.WriteLine("Servidor detenido.");
            }
        }

        private static void CrearBaseDeDatosYTabla()
        {
            // Verificar si la ruta de la base de datos está inicializada
            if (dbFilePath == null)
            {
                throw new InvalidOperationException("La ruta de la base de datos no se ha inicializado.");
            }

            // Verificar si la base de datos ya existe
            if (!File.Exists(dbFilePath))
            {
                // Crear la base de datos
                SQLiteConnection.CreateFile(dbFilePath);

                using (var connection = new SQLiteConnection($"Data Source={dbFilePath};"))
                {
                    connection.Open();
                    string sql = @"
                CREATE TABLE usuarios (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT NOT NULL,
                    phone TEXT,
                    dni TEXT NOT NULL,
                    birthdate TEXT NOT NULL
                );";
                    using var command = new SQLiteCommand(sql, connection);
                    command.ExecuteNonQuery(); // Crear la tabla
                }
                Console.WriteLine("Base de datos y tabla creadas.");
            }
            else
            {
                // Si la base de datos existe, verificar si la tabla 'usuarios' también existe
                using var connection = new SQLiteConnection($"Data Source={dbFilePath};");
                connection.Open();
                string checkTableSql = "SELECT name FROM sqlite_master WHERE type='table' AND name='usuarios';";
                using var command = new SQLiteCommand(checkTableSql, connection);
                var result = command.ExecuteScalar();
                if (result == null)
                {
                    // La tabla no existe, así que la creamos
                    string createTableSql = @"
                        CREATE TABLE usuarios (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            name TEXT NOT NULL,
                            email TEXT NOT NULL,
                            phone TEXT,
                            dni TEXT NOT NULL,
                            birthdate TEXT NOT NULL
                        );";
                    using (var createTableCommand = new SQLiteCommand(createTableSql, connection))
                    {
                        createTableCommand.ExecuteNonQuery();
                    }
                    Console.WriteLine("La tabla 'usuarios' fue creada.");
                }
                else
                {
                    // Contar el número de filas existentes en la tabla 'usuarios'
                    string countRowsSql = "SELECT COUNT(*) FROM usuarios;";
                    using var countCommand = new SQLiteCommand(countRowsSql, connection);
                    var rowCount = Convert.ToInt32(countCommand.ExecuteScalar());
                    contadorFilasInsertadas = rowCount; // Asignar el número de filas existentes al contador
                    Console.WriteLine($"La tabla 'usuarios' ya existe con {contadorFilasInsertadas} filas.");
                }
            }
        }

        private static void GuardarEnBaseDeDatos(string formData)
        {
            try
            {
                var parsedData = HttpUtility.ParseQueryString(formData);
                string name = parsedData["name"] ?? string.Empty;
                string email = parsedData["email"] ?? string.Empty;
                string phone = parsedData["phone"] ?? string.Empty;
                string dni = parsedData["dni"] ?? string.Empty;
                string birthdate = parsedData["birthdate"] ?? string.Empty;

                using var connection = new SQLiteConnection($"Data Source={dbFilePath};");
                connection.Open();
                Console.WriteLine("Conexión a la base de datos establecida.");  // Mensaje de depuración

                string sql = "INSERT INTO usuarios (name, email, phone, dni, birthdate) VALUES (@Name, @Email, @Phone, @DNI, @Birthdate)";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Phone", phone);
                command.Parameters.AddWithValue("@DNI", dni);
                command.Parameters.AddWithValue("@Birthdate", birthdate);

                int rowsAffected = command.ExecuteNonQuery();
                contadorFilasInsertadas += rowsAffected; // Incrementar el contador de filas insertadas
                Console.WriteLine($"Filas insertadas: {rowsAffected}");  // Verificar cuántas filas se insertaron
                Console.WriteLine($"Total de filas insertadas hasta ahora: {contadorFilasInsertadas}"); // Mostrar el total de filas insertadas
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar en la base de datos: {ex.Message}");
            }
        }

        // Método para obtener todos los usuarios en formato HTML
        private static string ObtenerUsuariosHTML()
        {
            StringBuilder html = new StringBuilder();
            html.Append("<html><body><h1>Lista de Usuarios</h1>");
            html.Append("<!-- Botón para acceder a /users -->\r\n    <button onclick=\"window.location.href='/';\">Registrar</button>");
            html.Append("<table border='1'><tr><th>ID</th><th>Nombre</th><th>Email</th><th>Movil</th><th>DNI</th><th>Fecha de Nacimiento</th></tr>");

            using var connection = new SQLiteConnection($"Data Source={dbFilePath};");
            connection.Open();

            string sql = "SELECT * FROM usuarios;";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                html.Append("<tr>");
                html.Append($"<td>{reader["id"]}</td>");
                html.Append($"<td>{reader["name"]}</td>");
                html.Append($"<td>{reader["email"]}</td>");
                html.Append($"<td>{reader["phone"]}</td>");
                html.Append($"<td>{reader["dni"]}</td>");
                html.Append($"<td>{reader["birthdate"]}</td>");
                html.Append("</tr>");
            }

            html.Append("</table></body></html>");
            return html.ToString();
        }
    }
}
