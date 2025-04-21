using System.Runtime.InteropServices;

namespace AccountingDomain;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct Transaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; }
}