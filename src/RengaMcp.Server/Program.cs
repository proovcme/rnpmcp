using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using RengaMcp.Adapters;
using RengaMcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    // stdout is reserved for the MCP protocol.
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IRengaAdapter>(services =>
{
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var mode = Environment.GetEnvironmentVariable("RENGA_MCP_MODE");

    return string.Equals(mode, "mock", StringComparison.OrdinalIgnoreCase)
        ? new MockRengaAdapter()
        : new DynamicComRengaAdapter(loggerFactory.CreateLogger<DynamicComRengaAdapter>());
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "renga-mcp", Version = "0.1.0" };
        options.ServerInstructions =
            "Read-only access to a running Renga BIM instance. Connect before reading a project. " +
            "Never infer regulatory compliance from model data alone. Results may be paginated or truncated.";
    })
    .WithStdioServerTransport()
    .WithTools<RengaTools>();

await builder.Build().RunAsync().ConfigureAwait(false);
