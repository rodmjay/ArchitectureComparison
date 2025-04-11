using System.ComponentModel.DataAnnotations;

namespace DataOrientedExample.Entities;

public class TransactionEntity
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }

    // Concurrency token to detect concurrent updates.
    [Timestamp]
    public byte[] RowVersion { get; set; }
}