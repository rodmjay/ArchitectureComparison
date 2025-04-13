using AccountingDomain;
using Benchmarks.Entities;
using Mapster;

namespace Benchmarks
{
    public static class MappingConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<Account, AccountEntity>.NewConfig()
                .Ignore(dest => dest.RowVersion);
            TypeAdapterConfig<Transaction, TransactionEntity>.NewConfig()
                .Ignore(dest=>dest.RowVersion);
            TypeAdapterConfig<Entry, EntryEntity>.NewConfig()
                .Ignore(dest => dest.RowVersion);
        }
    }
}