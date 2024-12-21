using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SystemRemoteService.Models;
using SystemRemoteService.Services;

namespace SystemRemoteService
{
    internal class WorkerHandles
    {
        internal static string ExecuteCommand(string command, bool logger = true)
        {
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

        internal static async Task DatabaseRequest(HttpListenerContext context)
        {
            string action = context.Request.QueryString["action"];
            string responseMessage;

            if (action == null)
            {
                responseMessage = "Action not specified.";
            }
            else
            {
                try
                {
                    switch (action.ToLower())
                    {
                        case "add":
                            string name = context.Request.QueryString["name"];
                            string description = context.Request.QueryString["description"];
                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
                            {
                                responseMessage = "Name and Description are required.";
                            }
                            else
                            {
                                var newCommand = new Command
                                {
                                    Id = new Random().Next(1, 1000), // Simples geração de ID
                                    Name = name,
                                    Description = description
                                };
                                DatabaseService.AddCommand(newCommand);
                                responseMessage = $"Command {newCommand.Name} added successfully.";
                            }
                            break;

                        case "update":
                            int id = int.Parse(context.Request.QueryString["id"]);
                            name = context.Request.QueryString["name"];
                            description = context.Request.QueryString["description"];
                            var updatedCommand = new Command
                            {
                                Id = id,
                                Name = name,
                                Description = description
                            };
                            DatabaseService.UpdateCommand(updatedCommand);
                            responseMessage = $"Command {updatedCommand.Name} updated successfully.";
                            break;

                        case "delete":
                            id = int.Parse(context.Request.QueryString["id"]);
                            DatabaseService.DeleteCommand(id);
                            responseMessage = $"Command with ID {id} deleted successfully.";
                            break;

                        default:
                            responseMessage = "Invalid action.";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    responseMessage = $"Error processing request: {ex.Message}";
                }
            }

            byte[] response = Encoding.UTF8.GetBytes(responseMessage);
            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }

        internal static async Task InfoRequest(HttpListenerContext context)
        {
            byte[] response = Encoding.UTF8.GetBytes(SystemInfo.GetSystemInformation());

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }

        internal static async Task CommandWebSocket(WebSocket socket, CancellationToken stoppingToken)
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

        internal static async Task TaskListRequest(HttpListenerContext context)
        {
            string result = ExecuteCommand("tasklist", false);
            byte[] response = Encoding.UTF8.GetBytes(result);

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }

        
    }
}
