namespace AccountingGraph;

public interface IMyPipelineService
{
    Task ProcessFileAsync(Stream requestBody);
}