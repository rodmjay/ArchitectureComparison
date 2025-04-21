using AccountingDomain;
using DataOrientedArchitecture.Data.Entities;
using HotChocolate.Types;

// For AccountType

namespace DataOrientedArchitecture.Data.ObjectTypes;

public class AccountEntityType : ObjectType<AccountEntity>
{
    protected override void Configure(IObjectTypeDescriptor<AccountEntity> descriptor)
    {
        descriptor.Description("Represents an account in the accounting system.");

        // Expose the Id, Name, and Type fields.
        descriptor.Field(a => a.Id)
            .Type<NonNullType<IntType>>();

        descriptor.Field(a => a.Name)
            .Type<NonNullType<StringType>>();

        descriptor.Field(a => a.Type)
            .Type<NonNullType<EnumType<AccountType>>>()
            .Description("The account type from the accounting domain.");

        // Do not expose the RowVersion.
        descriptor.Field(a => a.RowVersion)
            .Ignore();
    }
}