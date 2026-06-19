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

            var config = ConfigData.Load();
            Global.SetConfigData(config);

            // Logging config
            var consoleRule = LogManager.Configuration?.LoggingRules
                .FirstOrDefault(r => r.Targets.Any(t => t.Name == "console"));

            if (!config.Logging.EnableDebug)
            {
                consoleRule?.SetLoggingLevels(LogLevel.Info, LogLevel.Fatal);
                LogManager.ReconfigExistingLoggers();
            }

            // Validate IP
            if (string.IsNullOrWhiteSpace(config.Server.Address))
            {
                Log.Fatal("Incorrect server IP! Stopping...");
                await DatabaseManager.Stop();
                return;
            }

            // Certificate
            var certPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SSL",
                config.SSL.Certificate
            );

            var certDir = Path.GetDirectoryName(certPath);
            if (!Directory.Exists(certDir))
                Directory.CreateDirectory(certDir);

            if (!File.Exists(certPath))
            {
                Log.Warn("Certificate not found, generating new one...");
                CertGenerator.GenerateCert(config.SSL.Certificate);
            }

            var cert = new X509Certificate2(certPath);

            // Servers
            var tcpServer = new TCPServer(
                config.Server.Address,
                config.Server.TCP,
                cert
            );

            var httpServer = new HttpServer(
                config.Server.Address,
                config.Server.HTTP
            );

            var httpsServer = new HttpsServer(
                config.Server.Address,
                config.Server.HTTPS,
                cert
            );

            // STUN
            var stunTask = Task.Run(async () =>
            {
                var addresses = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .ToArray();

                foreach (var address in addresses)
                    Log.Info("Discovered IP: {0}", address);

                var endpoints = addresses
                    .Select(a => new IPEndPoint(a, config.Server.STUN))
                    .ToArray();

                var stunUdpServer = new StunUdpServer(endpoints);
                stunUdpServer.Start(config.Server.STUN);

                Log.Info("STUN server started on {0}", config.Server.STUN);
            });

            Log.Info("Starting servers...");

            var tasks = new[]
            {
                Task.Run(() => tcpServer.Start()),
                Task.Run(() => httpServer.Start()),
                Task.Run(() => httpsServer.Start()),
                stunTask
            };

            await Task.Delay(300);
            Log.Info("All servers started. Press Ctrl+C to stop.");

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

            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in server startup");
        }
    }
}