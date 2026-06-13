using System.Net;
using System.Security.Cryptography.X509Certificates;
using NLog;
using stungun.common.server;

class Program
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        try
        {
            LogManager.ThrowExceptions = true;
            
            await DatabaseManager.Init();
            Log.Info("Database has started");
            var Data = JsonReader.ReadJson<ConfigData>();
            Global.SetConfigData(Data);
            var consoleRule = LogManager.Configuration?.LoggingRules
                .FirstOrDefault(r => r.Targets.Any(t => t.Name == "console"));
            if(!Data.EnableDebug)
            {
                consoleRule?.SetLoggingLevels(LogLevel.Info, LogLevel.Fatal);
                LogManager.ReconfigExistingLoggers();
            }
            if(Data.ip is null || Data.ip == "")
            {
                Log.Fatal("Incorrect IP!\n Stopping servers...");
                await DatabaseManager.Stop();
                return;
            }
            string certFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cert");
            string certPath = Path.Combine(certFolder, Data.CertName ?? "server.pfx");
            if (!Directory.Exists(certFolder))
            {
                Directory.CreateDirectory(certFolder);
            }
            if (!File.Exists(certPath))
            {
                Log.Warn("Certificate file not found");
                CertGenerator.GenerateCert(Data.CertName ?? "server.pfx");
            }

            var cert = new X509Certificate2(certPath);

            Log.Info("Certificate loaded: {0}, expires {1}", cert.Subject, cert.GetExpirationDateString());

            // TCP server
            var tcpServer = new TCPServer(Data.ip, Data.TCPPort, cert);

            // HTTP server
            var httpServer = new HttpServer(Data.ip, Data.HTTPPort);

            // HTTPS server
            var httpsServer = new HttpsServer(Data.ip, Data.HTTPSPort, cert);

            // STUN server task
            var stunTask = Task.Run(async () =>
            {
                var addresses = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .ToArray();

                foreach (var address in addresses)
                    Log.Info("Discovered IP: {0}", address);

                var endpoints = addresses.Select(a => new IPEndPoint(a, Data.STUNPort)).ToArray();
                var stunUdpServer = new StunUdpServer(endpoints);
                stunUdpServer.Start(Data.STUNPort);
                Log.Info($"STUN server started on {Data.STUNPort}");
            });

            Log.Info("Starting servers...");

            // Run TCP, HTTP, HTTPS, STUN server in tasks
            var tasks = new[]
            {
                Task.Run(() => tcpServer.Start()),
                Task.Run(() => httpServer.Start()),
                Task.Run(() => httpsServer.Start()),
                stunTask
            };
            await Task.Delay(300);
            Log.Info("All servers started. Press Ctrl+C to stop.");
           
            // Graceful shutdown
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true;
                Log.Info("Stopping servers...");
                await Global.Stop();
                httpServer.Dispose();
                tcpServer.Dispose();
                httpsServer.Dispose();
                _ = DatabaseManager.Stop();
                LogManager.Shutdown();
                e.Cancel = false;
            };
            await Task.WhenAll(tasks);

            // Wait indefinitely
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in server startup");
        }
    }
}
