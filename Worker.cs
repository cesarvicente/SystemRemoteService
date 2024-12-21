using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Diagnostics;

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

            _logger.LogInformation("Server started at: http://localhost:8210");

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
                            _ = HandleWebSocket(wsContext.WebSocket, stoppingToken);
                            break;

                        case "/tasklist":
                            await HandleTaskListRequest(context);
                            break;

                        case "/info":
                            await HandleInfoRequest(context);
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

        private async Task HandleInfoRequest(HttpListenerContext context)
        {
            byte[] response = Encoding.UTF8.GetBytes(SystemInfo.GetSystemInformation());

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
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

        private async Task HandleTaskListRequest(HttpListenerContext context)
        {
            string result = ExecuteCommand("tasklist", false);
            byte[] response = Encoding.UTF8.GetBytes(result);

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }

        private string ExecuteCommand(string command, bool logger = true)
        {
            if (logger) _logger.LogInformation($"{DateTime.Now} | Command: {command}");
            try
            {
                using var process = new Process();
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
                return $"Erro: {ex.Message}";
            }
        }
    }
}
