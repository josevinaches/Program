using ServidorLib;

namespace Program
{
    private static void Main(string[] args)
    {
        HttpServer server = new HttpServer(8080);
        server.Start();

        Console.WriteLine("Presiona cualquier tecla para detener el servidor...");
        Console.ReadKey();

        server.Stop();
    }
}
