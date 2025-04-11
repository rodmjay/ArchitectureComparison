// Unique identifier types for clarity (could also use int directly)

using AccountId = int;

namespace DataOrientedExample.Domain;

public struct Account
{
    public AccountId Id;
    public string Name;
    public AccountType Type;  // Asset, Liability, etc., maybe an enum
}