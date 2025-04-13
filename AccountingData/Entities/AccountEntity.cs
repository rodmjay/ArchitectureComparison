using System.ComponentModel.DataAnnotations;
using AccountingDomain;

namespace Benchmarks.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public AccountType Type { get; set; }

    // This property will be used by EF Core as a concurrency token.
    [Timestamp]
    public byte[] RowVersion { get; set; }
}