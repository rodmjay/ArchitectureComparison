using System.ComponentModel.DataAnnotations;

namespace DataOrientedArchitecture.Data.Entities;

public class EntryEntity
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}