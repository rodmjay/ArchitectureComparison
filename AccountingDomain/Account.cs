// Unique identifier types for clarity (could also use int directly)

using System.Runtime.InteropServices;
using AccountId = int;

namespace AccountingDomain;

[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct Account
{
    public AccountId Id { get; set; }
    public string Name { get; set; }
    public AccountType Type { get; set; } 
}