namespace DataOrientedExample.Domain;

public struct Entry
{
    public int TransactionId;
    public int AccountId;
    public decimal Amount;
    // You could include e.g. a short memo or date, but keep it minimal for hot data
}