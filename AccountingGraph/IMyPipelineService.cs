public interface IMyPipelineService
{
    Task ProcessFileAsync(Stream requestBody);
}