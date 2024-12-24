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
using System.Text.Json;

namespace SystemRemoteService
{
    internal class WorkerHandles
    {
        internal static string ExecuteCommand(string command)
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

        public static void AddOrUpdateFirewallRule(int port)
        {
            string ruleName = $"System Remote Service";

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(processStartInfo);
                var output = process!.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (output.Contains("No rules"))
                {
                    AddFirewallRule(port);
                }
                else
                {
                    if (!output.Contains($"LocalPort               {port}"))
                    {
                        ModifyFirewallRule(ruleName, port);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar ou adicionar a regra no firewall: {ex.Message}");
            }
        }

        private static void AddFirewallRule(int port)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"System Remote Service\" dir=in action=allow protocol=TCP localport={port}",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar a regra no firewall: {ex.Message}");
            }
        }

        private static void ModifyFirewallRule(string ruleName, int port)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall set rule name=\"{ruleName}\" new localport={port}",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao modificar a regra no firewall: {ex.Message}");
            }
        }

        internal static async Task DatabaseRequest(HttpListenerContext context)
        {
            string? action = context.Request.QueryString["action"];
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
                            string? name = context.Request.QueryString[nameof(Command.Prompt)];
                            string? description = context.Request.QueryString[nameof(Command.Description)];
                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
                            {
                                responseMessage = "Prompt and Description are required.";
                            }
                            else
                            {
                                var newCommand = new Command
                                {
                                    Prompt = name,
                                    Description = description
                                };
                                DatabaseService.AddCommand(newCommand);
                                responseMessage = $"Command {newCommand.Prompt} added successfully.";
                            }
                            break;

                        case "update":
                            string? id = context.Request.QueryString["id"];
                            name = context.Request.QueryString[nameof(Command.Prompt)];
                            description = context.Request.QueryString[nameof(Command.Description)];
                            var updatedCommand = new Command
                            {
                                Id = id!,
                                Prompt = name!,
                                Description = description!
                            };
                            DatabaseService.UpdateCommand(updatedCommand);
                            responseMessage = $"Command {updatedCommand.Prompt} updated successfully.";
                            break;

                        case "delete":
                            id = context.Request.QueryString["id"];
                            DatabaseService.DeleteCommand(id!);
                            responseMessage = $"Command with ID {id} deleted successfully.";
                            break;

                        case "list":
                            var items = DatabaseService.LoadCommands();
                            responseMessage = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
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
            string result = ExecuteCommand("tasklist");
            byte[] response = Encoding.UTF8.GetBytes(result);

            context.Response.StatusCode = 200;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();
        }
    }
}
