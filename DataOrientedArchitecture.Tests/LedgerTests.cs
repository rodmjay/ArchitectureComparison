using System;
using System.Linq;
using AccountingDomain;
using NUnit.Framework;

namespace DataOrientedArchitecture.Tests
{
    [TestFixture]
    public class LedgerTests
    {
        protected Ledger ledger;

        [SetUp]
        public void SetUp()
        {
            ledger = new Ledger();
        }

        [TestFixture]
        private class AddAccountTests : LedgerTests
        {
            [Test]
            public void ShouldReturnCorrectId()
            {
                // Arrange & Act
                int id1 = ledger.AddAccount("Cash", AccountType.Asset);
                int id2 = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

                // Assert
                Assert.That(id1, Is.EqualTo(0), "The first account ID should be 0.");
                Assert.That(id2, Is.EqualTo(1), "The second account ID should be 1.");
                Assert.That(ledger.Accounts.Count, Is.EqualTo(2),
                    "There should be exactly two accounts in the ledger.");
            }
        }

        [TestFixture]
        private class PostTransactionTests : LedgerTests
        {
            [Test]
            public void WithValidData_ShouldAddTransactionAndEntries()
            {
                // Arrange
                int cashId = ledger.AddAccount("Cash", AccountType.Asset);
                int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

                // Act
                int tranId = ledger.PostTransaction(
                    DateTime.UtcNow, "Sale",
                    cashId, 100m,
                    revenueId, 100m);

                // Assert
                Assert.That(ledger.Transactions.Count, Is.EqualTo(1),
                    "There should be exactly one transaction.");
                Assert.That(ledger.Entries.Count, Is.EqualTo(2),
                    "A valid transaction creates two entries: debit & credit.");
                Assert.That(ledger.Transactions.First().Id, Is.EqualTo(tranId),
                    "The transaction ID should match the returned ID.");
            }

            [Test]
            public void WithUnbalancedAmounts_ShouldThrowInvalidOperationException()
            {
                // Arrange
                int cashId = ledger.AddAccount("Cash", AccountType.Asset);
                int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() =>
                    ledger.PostTransaction(
                        DateTime.UtcNow, "Sale",
                        cashId, 100m,
                        revenueId, 90m),
                    "Unbalanced debit/credit should raise InvalidOperationException.");
            }
        }

        [TestFixture]
        private class RecalculateBalanceTests : LedgerTests
        {
            [Test]
            public void SequentialAndParallel_ShouldReturnSameResult()
            {
                // Arrange
                int cashId = ledger.AddAccount("Cash", AccountType.Asset);
                int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

                ledger.PostTransaction(DateTime.UtcNow, "T1", cashId, 100m, revenueId, 100m);
                ledger.PostTransaction(DateTime.UtcNow, "T2", cashId, 200m, revenueId, 200m);

                // Act
                decimal seqBalance = ledger.RecalculateAccountBalanceSequential(cashId);
                decimal parBalance = ledger.RecalculateAccountBalanceParallel(cashId);

                // Assert
                Assert.That(parBalance, Is.EqualTo(seqBalance),
                    "Parallel and sequential balances must match.");
                Assert.That(seqBalance, Is.EqualTo(300m),
                    "The computed balance for Cash should be 300.");
            }
        }

        [TestFixture]
        private class SnapshotTests : LedgerTests
        {
            [Test]
            public void Snapshot_ShouldReturnIndependentCopy()
            {
                // Arrange
                int cashId = ledger.AddAccount("Cash", AccountType.Asset);
                int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

                ledger.PostTransaction(DateTime.UtcNow, "T1", cashId, 100m, revenueId, 100m);
                ledger.PostTransaction(DateTime.UtcNow, "T2", cashId, 200m, revenueId, 200m);

                // Act
                var snapshot = ledger.Snapshot();

                // Assert count matches
                Assert.That(snapshot.Length, Is.EqualTo(ledger.Transactions.Count),
                    "Snapshot should contain same number of transactions.");

                // Mutate ledger and verify snapshot stays the same
                ledger.Transactions.Add(new Transaction { Id = 999, Date = DateTime.UtcNow, Description = "T3" });
                Assert.That(snapshot.Length, Is.Not.EqualTo(ledger.Transactions.Count),
                    "Snapshot must not reflect later changes to ledger.Transactions.");
            }
        }

        [TestFixture]
        private class DeleteEntryTests : LedgerTests
        {
            [Test]
            public void DeleteEntry_ShouldRemoveEntryAndTrackKey()
            {
                int a1 = ledger.AddAccount("A1", AccountType.Asset);
                int a2 = ledger.AddAccount("A2", AccountType.Liability);
                int tx = ledger.PostTransaction(DateTime.UtcNow, "X", a1, 50m, a2, 50m);

                // Precondition
                Assert.That(ledger.Entries, Has.Exactly(1)
                    .Property("TransactionId").EqualTo(tx)
                    .And.Property("AccountId").EqualTo(a1));

                // Act
                ledger.DeleteEntry(tx, a1);

                // Entry removed
                Assert.That(
                    ledger.Entries.Any(e => e.TransactionId == tx && e.AccountId == a1),
                    Is.False,
                    "The entry should have been removed from the ledger."
                );

                // Key tracked
                Assert.That(ledger.DeletedEntryKeys,
                    Contains.Item((tx, a1)));
            }
        }

        [TestFixture]
        private class DeleteTransactionTests : LedgerTests
        {
            [Test]
            public void DeleteTransaction_ShouldRemoveTransactionEntriesAndTrackIds()
            {
                int a1 = ledger.AddAccount("A1", AccountType.Asset);
                int a2 = ledger.AddAccount("A2", AccountType.Liability);
                int tx1 = ledger.PostTransaction(DateTime.UtcNow, "X1", a1, 20m, a2, 20m);
                int tx2 = ledger.PostTransaction(DateTime.UtcNow, "X2", a1, 30m, a2, 30m);

                // Act
                ledger.DeleteTransaction(tx1);

                // Transaction removed
                Assert.That(ledger.Transactions.Select(t => t.Id), Does.Not.Contain(tx1));

                // Entries removed
                Assert.That(ledger.Entries,
                    Has.None.Matches<Entry>(e => e.TransactionId == tx1));

                // Keys tracked
                Assert.That(ledger.DeletedTransactionIds, Contains.Item(tx1));
                Assert.That(ledger.DeletedEntryKeys,
                    Contains.Item((tx1, a1))
                    .And.Contains((tx1, a2)));
            }
        }

        [TestFixture]
        private class DeleteAccountTests : LedgerTests
        {
            [Test]
            public void DeleteAccount_ShouldRemoveAccountEntriesAndTrackIds()
            {
                int a1 = ledger.AddAccount("A1", AccountType.Asset);
                int a2 = ledger.AddAccount("A2", AccountType.Liability);
                int tx1 = ledger.PostTransaction(DateTime.UtcNow, "X1", a1, 15m, a2, 15m);

                // Act
                ledger.DeleteAccount(a1);

                // Account removed
                Assert.That(ledger.Accounts.Select(a => a.Id), Does.Not.Contain(a1));

                // Entries removed
                Assert.That(ledger.Entries,
                    Has.None.Matches<Entry>(e => e.AccountId == a1));

                // Keys tracked
                Assert.That(ledger.DeletedAccountIds, Contains.Item(a1));
                Assert.That(ledger.DeletedEntryKeys, Contains.Item((tx1, a1)));
            }
        }
    }
}
