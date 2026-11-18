using System.Text.Json.Serialization;
using MyCodingZone.WebApi.Api;
using Siemens.AspNet.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// Register API endpoints and services
builder.Services.AddApi(builder.Configuration);

// Error handling - enterprise ready (Siemens.AspNet.ErrorHandling)
// RFC 7807 ProblemDetails for every exception, ValidationProblemDetailsExtended for invalid request bodies
// (current value, allowed samples and the location of the broken JSON property)
builder.Services.AddErrorHandling(builder.Configuration);

// Enums as strings in requests and responses
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Configure pipeline - the error handler comes first, so it sees every exception of the pipeline
app.UseErrorHandling();
app.UseHttpsRedirection();

// Map API endpoints
app.MapApi();

app.Run();

// Needed for integration tests via WebApplicationFactory<Program>
namespace MyCodingZone.WebApi
{
#pragma warning disable CA1515
    public class Program;
#pragma warning restore CA1515
}
