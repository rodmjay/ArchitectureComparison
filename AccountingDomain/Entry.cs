using System.Runtime.InteropServices;

namespace AccountingDomain;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct Entry
{
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
}