using Microsoft.EntityFrameworkCore;

namespace AccountingDataPipeline.Data;

public class SimpleDbContextFactory : IDbContextFactory<PipelineDbContext>
{
    private readonly DbContextOptions<PipelineDbContext> _options;

    public SimpleDbContextFactory(DbContextOptions<PipelineDbContext> options)
    {
        _options = options;
    }

    public PipelineDbContext CreateDbContext()
    {
        return new PipelineDbContext(_options);
    }
}