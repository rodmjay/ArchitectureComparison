using AccountingDataPipeline;
using AccountingDataPipeline.Data;
using AccountingDataPipeline.Parsing;
using AccountingDataPipeline.Pipeline;
using AccountingDataPipeline.Processing;
using AccountingDataPipeline.Sinks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// (Optional) Register an EF Core DbContext if you need it for your domain.
// Replace the connection string with your own if needed.
builder.Services.AddDbContextPool<PipelineDbContext>(options =>
    options.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=Pipeline;Integrated Security=true;")
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
// Register pipeline components in DI
builder.Services.AddSingleton<IJsonParser, SystemTextJsonStreamParser>();
builder.Services.AddSingleton<IRecordTransformer<Record, Record>, SampleRecordTransformer>();
builder.Services.AddSingleton<IRecordSink<Record>, ConsoleRecordSink>();
builder.Services.AddSingleton<LargeFileProcessingPipeline>();

var app = builder.Build();

app.MapPost("/upload", async (HttpContext context, LargeFileProcessingPipeline pipeline) =>
{
    // For simplicity, we assume that the entire JSON file is provided as the raw request body.
    // (Adjust if you are using multipart/form-data.)
    await pipeline.ProcessFileAsync(context.Request.Body);
    return Results.Ok("File processing complete.");
});

app.Run();