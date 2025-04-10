namespace DataOrientedExample;

public struct Transaction
{
    public int Id;
    public DateTime Date;
    public string Description;
    // We might not store pointers to entries; instead, we'll find entries by Id.
}