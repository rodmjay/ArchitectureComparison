using DataOrientedArchitecture.Data.Entities;
using HotChocolate.Types;

namespace DataOrientedArchitecture.Data.ObjectTypes;

public class TransactionEntityType : ObjectType<TransactionEntity>
{
    protected override void Configure(IObjectTypeDescriptor<TransactionEntity> descriptor)
    {
        descriptor.Description("Represents a financial transaction with a date and description.");

        descriptor.Field(t => t.Id)
            .Type<NonNullType<IntType>>();

        descriptor.Field(t => t.Date)
            .Type<NonNullType<DateTimeType>>();

        descriptor.Field(t => t.Description)
            .Type<NonNullType<StringType>>();

        // Hide the concurrency token.
        descriptor.Field(t => t.RowVersion)
            .Ignore();

        // OPTIONAL: You could add a field to resolve related entries if needed.
        // descriptor.Field("entries")
        //           .ResolveWith<TransactionResolvers>(r => r.GetEntries(default!, default))
        //           .Type<ListType<NonNullType<EntryEntityType>>>()
        //           .Description("The entries associated with this transaction.");
    }
}