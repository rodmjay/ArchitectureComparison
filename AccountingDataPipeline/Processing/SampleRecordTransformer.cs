namespace AccountingDataPipeline.Processing;

public class SampleRecordTransformer : IRecordTransformer<Record, Record>
{
    public Record Transform(Record input)
    {
        // In a real scenario, you might adjust properties or convert from a DTO.
        // For this PoC, just return the input.
        return input;
    }
}