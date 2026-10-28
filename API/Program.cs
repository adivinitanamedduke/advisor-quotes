using System.Text.Json.Serialization;
using API.Filters;
using API.Middleware;
using API.Middleware.DependencyInjection;
using API.Observability;
using Asp.Versioning;
using Business.Abstractions.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model binding failures (malformed JSON, wrong types) use the same error body as everything else
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError(
                    entry.Key,
                    string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value" : error.ErrorMessage)))
                .ToList();

            return ApiExceptionHandlerAttribute.ErrorResult(
                StatusCodes.Status400BadRequest,
                string.Join("; ", errors.Select(e => e.Message)),
                errors);
        };
    });

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddDataLayer(builder.Configuration)
    .AddPricingModule(builder.Configuration)
    .AddQuotesModule(builder.Configuration);

var app = builder.Build();

await app.MigrateIfEnabledAsync();

app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();

// Lets WebApplicationFactory<Program> find the entry point in integration tests
public partial class Program;
