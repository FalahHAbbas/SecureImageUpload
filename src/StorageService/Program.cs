using StorageService.Controllers;
using StorageService;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Instrumentation.AspNetCore;

using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Prometheus;



var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(context.Configuration["Loki:Url"] ?? "http://loki:3100"));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key for authentication",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] { }
        }
    });
});

var hmacSettings = builder.Configuration.GetSection("HmacSettings").Get<HmacSettings>();
if (hmacSettings != null) builder.Services.AddSingleton(hmacSettings);
builder.Services.AddSingleton<IHmacSignatureValidator, HmacSignatureValidator>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("StorageService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddZipkinExporter(o =>
        {
            o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
        }))
    .WithMetrics(metrics => metrics
        .AddPrometheusExporter()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMetricServer();

app.MapControllers();

app.Run();