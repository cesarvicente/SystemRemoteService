using SystemRemoteService;

var builder = Host.CreateDefaultBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.UseWindowsService();
}

builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<Worker>();
});

var host = builder.Build();
host.Run();
