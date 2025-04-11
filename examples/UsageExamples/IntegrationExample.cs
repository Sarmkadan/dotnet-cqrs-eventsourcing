using Microsoft.Extensions.DependencyInjection;
using DotNetCqrsEventSourcing.Configuration;
using DotNetCqrsEventSourcing.Application.Services;
using DotNetCqrsEventSourcing.Data.Repositories;

// Integration example showing how to wire the framework into ASP.NET Core DI.

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// Register the framework in standard ASP.NET Core Dependency Injection
builder.Services.AddCqrsFramework();

// Register infrastructure components
builder.Services.AddSingleton<IEventRepository, InMemoryEventRepository>();

using var host = builder.Build();

// Usage within a controller or service via DI
var accountService = host.Services.GetRequiredService<IAccountService>();

Console.WriteLine("Framework integrated with Host DI successfully.");
