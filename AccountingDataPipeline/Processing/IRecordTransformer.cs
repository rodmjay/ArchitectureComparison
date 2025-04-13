namespace AccountingDataPipeline.Processing;

public interface IRecordTransformer<TInput, TOutput>
{
    TOutput Transform(TInput input);
}