using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using SystemRemoteService.Models;
using SystemRemoteService.Services;
using SystemRemoteService;

namespace SystemRemoteService
{
    public class Worker : BackgroundService
    {
        public readonly ILogger<Worker> _logger;

        private readonly int _port;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();

            _port = configuration.GetValue<int>("port");

            WorkerHandles.AddOrUpdateFirewallRule(_port);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{_port}/");
            listener.Start();

            _logger.LogInformation($"Server started at: http://0.0.0.0:{_port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();

                    switch (context.Request.Url!.AbsolutePath)
                    {
                        case "/command":
                            if (!context.Request.IsWebSocketRequest) CloseResponse();
                            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                            _ = WorkerHandles.CommandWebSocket(wsContext.WebSocket, stoppingToken);
                            break;

                        case "/tasklist":
                            await WorkerHandles.TaskListRequest(context);
                            break;

                        case "/info":
                            await WorkerHandles.InfoRequest(context);
                            break;

                        case "/database":
                            await WorkerHandles.DatabaseRequest(context);
                            break;

                        default:
                            CloseResponse();
                            break;
                    }

                    void CloseResponse()
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error on HTTP server: {ex.Message}");
                }
            }

            listener.Stop();
            _logger.LogInformation("Server stopped.");
        }
    }
}
