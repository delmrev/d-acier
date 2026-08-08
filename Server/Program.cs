using System.Net;
using System.Security.Cryptography.X509Certificates;
using Database;
using NLog;

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
            GlobalManager.Instance.SetConfig(config);
            AutomatchManager.Instance.config = config;
            
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
                config,
                cert
            );

            var httpServer = new HttpServer(
                config
            );

            var httpsServer = new HttpsServer(
                config,
                cert
            );

            // STUN
            var stunTask = Task.Run(async () =>
            {
                Log.Info("STUN server started");
                await StunServerManager.Instance.Init(StunConfig.Load());
            });

            Log.Info("Starting servers...");

            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            var tasks = new[]
            {
                Task.Run(() => tcpServer.Start()),
                Task.Run(() => httpServer.Start()),
                Task.Run(() => httpsServer.Start()),
                stunTask
            };

            await Task.Delay(300);
            Log.Info("All servers started. Press Ctrl+C to stop.");

            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(Timeout.Infinite, cts.Token));

            Log.Info("Stopping servers...");

            await GlobalManager.Instance.Stop();
            httpServer.Dispose();
            tcpServer.Dispose();
            httpsServer.Dispose();

            _ = DatabaseManager.Stop();
            LogManager.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in server startup");
        }
    }
}