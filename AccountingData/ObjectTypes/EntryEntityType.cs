using Benchmarks.Entities;
using HotChocolate.Types;

namespace Benchmarks.ObjectTypes;

public class EntryEntityType : ObjectType<EntryEntity>
{
    protected override void Configure(IObjectTypeDescriptor<EntryEntity> descriptor)
    {
        descriptor.Description("Represents a journal entry line within a transaction.");

        descriptor.Field(e => e.Id)
            .Type<NonNullType<IntType>>();

        descriptor.Field(e => e.TransactionId)
            .Type<NonNullType<IntType>>();

        descriptor.Field(e => e.AccountId)
            .Type<NonNullType<IntType>>();

        descriptor.Field(e => e.Amount)
            .Type<NonNullType<DecimalType>>();

        // Hide concurrency token.
        descriptor.Field(e => e.RowVersion)
            .Ignore();
    }
}