using System.ComponentModel.DataAnnotations;

namespace DataOrientedExample;

public class EntryEntity
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }

    // Optionally add a rowversion if you intend to update entries.
    [Timestamp]
    public byte[] RowVersion { get; set; }
}