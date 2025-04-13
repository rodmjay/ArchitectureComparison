using AccountingData.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Create the builder.
var builder = WebApplication.CreateBuilder(args);

// --- Register Application Services ---

// (Optional) Register an EF Core DbContext if you need it for your domain.
// Replace the connection string with your own if needed.
builder.Services.AddDbContextPool<LedgerContext>(options =>
    options.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// (Optional) Register your repositories or any other services.
builder.Services.AddScoped<LedgerRepository>();

// --- Register GraphQL with HotChocolate ---
// Make sure you have created your Query and Mutation classes.
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

// (Optional) Register controllers if you plan to add additional REST endpoints.
builder.Services.AddControllers();

// --- Build the app ---
var app = builder.Build();

// --- Map Endpoints ---

//// Sample REST endpoint.
//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
//    "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//            new WeatherForecast
//            (
//                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//                Random.Shared.Next(-20, 55),
//                summaries[Random.Shared.Next(summaries.Length)]
//            ))
//        .ToArray();
//    return forecast;
//});

// Map the GraphQL endpoint.
app.MapGraphQL("/graphql");

app.MapPost("/upload", async (HttpRequest request, IMyPipelineService pipeline) =>
{
    // Assume request content is multipart/form-data with a file, or just raw JSON in body
    // If it's multipart, use Request.Form.Files to get the IFormFile.
    await pipeline.ProcessFileAsync(request.Body);
    return Results.Ok("File processing started");
});

// (Optional) Map controllers.
//app.MapControllers();

app.Run();