namespace AccountingDataPipeline
{
    public class Record
    {
        // Change the type from int? to long? (or long) so it matches SQL Server's bigint.
        public long? Id { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
    }
}