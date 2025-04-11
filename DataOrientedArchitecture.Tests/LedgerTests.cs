using NUnit.Framework;
using DataOrientedExample.Domain;
using System;
using System.Linq;
using NUnit.Framework.Legacy;

namespace DataOrientedArchitecture.Tests
{
    [TestFixture]
    public class LedgerTests
    {
        private Ledger ledger;

        [SetUp]
        public void SetUp()
        {
            // Create a new Ledger for each test.
            ledger = new Ledger();
        }

        [Test]
        public void AddAccount_ShouldReturnCorrectId()
        {
            int id1 = ledger.AddAccount("Cash", AccountType.Asset);
            int id2 = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            ClassicAssert.AreEqual(0, id1, "The first account ID should be 0.");
            ClassicAssert.AreEqual(1, id2, "The second account ID should be 1.");
            ClassicAssert.AreEqual(2, ledger.Accounts.Count, "There should be exactly two accounts in the ledger.");
        }

        [Test]
        public void PostTransaction_WithValidData_ShouldAddTransactionAndEntries()
        {
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            int tranId = ledger.PostTransaction(DateTime.UtcNow, "Sale", cashId, 100m, revenueId, 100m);

            ClassicAssert.AreEqual(1, ledger.Transactions.Count, "There should be exactly one transaction.");
            // A valid transaction creates 2 entries: one debit and one credit.
            ClassicAssert.AreEqual(2, ledger.Entries.Count, "There should be two ledger entries for the transaction.");
            ClassicAssert.AreEqual(tranId, ledger.Transactions.First().Id, "The transaction ID should match.");
        }

        [Test]
        public void PostTransaction_WithUnbalancedAmounts_ShouldThrowException()
        {
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            // The amounts are unbalanced: 100 vs 90.
            Assert.Throws<InvalidOperationException>(() =>
            {
                ledger.PostTransaction(DateTime.UtcNow, "Sale", cashId, 100m, revenueId, 90m);
            }, "An InvalidOperationException is expected when debit and credit amounts do not match.");
        }

        [Test]
        public void RecalculateAccountBalance_SequentialAndParallel_ShouldReturnSameResult()
        {
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            // Post two transactions:
            // Transaction 1: +100 for Cash, -100 for Revenue.
            // Transaction 2: +200 for Cash, -200 for Revenue.
            ledger.PostTransaction(DateTime.UtcNow, "T1", cashId, 100m, revenueId, 100m);
            ledger.PostTransaction(DateTime.UtcNow, "T2", cashId, 200m, revenueId, 200m);

            // Expected balance for Cash: 300.
            decimal sequentialBalance = ledger.RecalculateAccountBalanceSequential(cashId);
            decimal parallelBalance = ledger.RecalculateAccountBalanceParallel(cashId);

            Assert.That(sequentialBalance, Is.EqualTo(parallelBalance), "Both methods should yield the same account balance.");
            ClassicAssert.AreEqual(300m, sequentialBalance, "The computed balance for the Cash account should be 300.");
        }

        [Test]
        public void Snapshot_ShouldReturnCopyOfTransactions()
        {
            // Post two transactions to populate the ledger.
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);
            ledger.PostTransaction(DateTime.UtcNow, "T1", cashId, 100m, revenueId, 100m);
            ledger.PostTransaction(DateTime.UtcNow, "T2", cashId, 200m, revenueId, 200m);

            ReadOnlySpan<Transaction> snapshot = ledger.Snapshot();

            // Verify that the snapshot contains the same number of transactions.
            ClassicAssert.AreEqual(ledger.Transactions.Count, snapshot.Length, "Snapshot should contain the same number of transactions as the ledger.");

            // Modifying the ledger after taking a snapshot should not change the snapshot data.
            ledger.Transactions.Add(new Transaction { Id = 999, Date = DateTime.UtcNow, Description = "T3" });
            ClassicAssert.AreNotEqual(ledger.Transactions.Count, snapshot.Length, "The snapshot should remain unchanged even if the ledger is modified later.");
        }
    }
}
