using System.Text.Json.Serialization;
using AscentService.Application;
using AscentService.Infrastructure;
using AscentService.Infrastructure.Persistence;
using Common.API.Documentation;
using Common.API.Health;
using Common.API.Middlewares;
using Common.API.Security;
using Common.Application.Abstractions;
using Common.Infrastructure.Observability;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddCommonSerilog("ascent-service");
builder.AddCommonOpenTelemetry("ascent-service");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddCommonJwtAuthentication(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCommonSwagger("ascent-service");

builder.Services.AddHealthChecks().AddDbContextCheck<AscentDbContext>();

WebApplication app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCommonHealthChecks();

await app.RunAsync();
