using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingDomain
{
    /// <summary>
    /// Represents an in-memory double-entry accounting ledger with support
    /// for posting, searching, and tracking deletions.
    /// </summary>
    public class Ledger
    {
        private int _nextTransactionId = 1;

        /// <summary>
        /// All active accounts in the ledger.
        /// </summary>
        public List<Account> Accounts { get; } = new();

        /// <summary>
        /// All active transactions in the ledger.
        /// </summary>
        public List<Transaction> Transactions { get; } = new();

        /// <summary>
        /// All active entry lines in the ledger.
        /// </summary>
        public List<Entry> Entries { get; } = new();

        /// <summary>
        /// Keys of accounts that have been deleted.
        /// </summary>
        public HashSet<int> DeletedAccountIds { get; } = new();

        /// <summary>
        /// Keys of transactions that have been deleted.
        /// </summary>
        public HashSet<int> DeletedTransactionIds { get; } = new();

        /// <summary>
        /// Keys of entry lines that have been deleted (transactionId, accountId).
        /// </summary>
        public HashSet<(int TransactionId, int AccountId)> DeletedEntryKeys { get; } = new();

        /// <summary>
        /// Creates a new account and returns its generated identifier.
        /// </summary>
        public int AddAccount(string name, AccountType type)
        {
            int id = Accounts.Count;
            Accounts.Add(new Account { Id = id, Name = name, Type = type });
            return id;
        }

        /// <summary>
        /// Deletes an account and any related entries.
        /// Tracks the deleted account ID in <see cref="DeletedAccountIds"/>.
        /// </summary>
        public void DeleteAccount(int accountId)
        {
            if (Accounts.RemoveAll(a => a.Id == accountId) > 0)
                DeletedAccountIds.Add(accountId);

            // Purge entries tied to that account
            var keys = Entries
                .Where(e => e.AccountId == accountId)
                .Select(e => (e.TransactionId, e.AccountId))
                .ToList();
            foreach (var key in keys)
                DeleteEntry(key.TransactionId, key.AccountId);
        }

        /// <summary>
        /// Deletes a transaction and any related entries.
        /// Tracks the deleted transaction ID in <see cref="DeletedTransactionIds"/>.
        /// </summary>
        public void DeleteTransaction(int transactionId)
        {
            if (Transactions.RemoveAll(t => t.Id == transactionId) > 0)
                DeletedTransactionIds.Add(transactionId);

            // Purge entries tied to that transaction
            var keys = Entries
                .Where(e => e.TransactionId == transactionId)
                .Select(e => (e.TransactionId, e.AccountId))
                .ToList();
            foreach (var key in keys)
                DeleteEntry(key.TransactionId, key.AccountId);
        }

        /// <summary>
        /// Deletes a single entry line by its transaction and account IDs.
        /// Tracks the key tuple in <see cref="DeletedEntryKeys"/>.
        /// </summary>
        public void DeleteEntry(int transactionId, int accountId)
        {
            if (Entries.RemoveAll(e =>
                    e.TransactionId == transactionId &&
                    e.AccountId == accountId) > 0)
            {
                DeletedEntryKeys.Add((transactionId, accountId));
            }
        }

        /// <summary>
        /// Posts a balanced transaction with one debit and one credit entry.
        /// Returns the generated transaction ID.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if amounts are non-positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown if debit and credit amounts differ.</exception>
        public int PostTransaction(
            DateTime date,
            string description,
            int debitAccount,
            decimal debitAmount,
            int creditAccount,
            decimal creditAmount)
        {
            if (debitAmount <= 0 || creditAmount <= 0)
                throw new ArgumentException("Amounts must be positive.");
            if (debitAmount != creditAmount)
                throw new InvalidOperationException("Debit and credit must balance.");

            int id = _nextTransactionId++;
            Transactions.Add(new Transaction
            {
                Id = id,
                Date = date,
                Description = description
            });

            // Debit entry (positive amount)
            Entries.Add(new Entry
            {
                TransactionId = id,
                AccountId = debitAccount,
                Amount = debitAmount
            });

            // Credit entry (negative amount)
            Entries.Add(new Entry
            {
                TransactionId = id,
                AccountId = creditAccount,
                Amount = -creditAmount
            });

            return id;
        }

        /// <summary>
        /// Searches transactions by a predicate and returns a ReadOnlySpan of matches.
        /// </summary>
        public ReadOnlySpan<Transaction> SearchTransactions(Func<Transaction, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            Transaction[] matches = Transactions.Where(predicate).ToArray();
            return new ReadOnlySpan<Transaction>(matches);
        }

        /// <summary>
        /// Searches transactions by a predicate and returns a List of matches.
        /// </summary>
        public List<Transaction> SearchTransactionsCopy(Func<Transaction, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Transactions.Where(predicate).ToList();
        }

        /// <summary>
        /// Computes the balance of an account by summing its entries sequentially.
        /// </summary>
        public decimal RecalculateAccountBalanceSequential(int accountId)
        {
            decimal total = 0m;
            foreach (var e in Entries)
                if (e.AccountId == accountId)
                    total += e.Amount;
            return total;
        }

        /// <summary>
        /// Computes the balance of an account by summing its entries in parallel.
        /// </summary>
        public decimal RecalculateAccountBalanceParallel(int accountId)
        {
            decimal result = 0m;
            object lockObj = new();

            Parallel.ForEach(
                Entries,
                () => 0m,
                (entry, state, local) => entry.AccountId == accountId ? local + entry.Amount : local,
                local =>
                {
                    lock (lockObj)
                    {
                        result += local;
                    }
                });

            return result;
        }

        /// <summary>
        /// Builds an interaction matrix [debitAccountId, creditAccountId] -> total amount.
        /// </summary>
        public decimal[,] CalculateInteractionMatrix()
        {
            int n = Accounts.Count;
            var matrix = new decimal[n, n];

            foreach (var group in Entries.GroupBy(e => e.TransactionId))
            {
                var list = group.ToList();
                if (list.Count == 2)
                {
                    var debit = list.First(e => e.Amount > 0);
                    var credit = list.First(e => e.Amount < 0);
                    matrix[debit.AccountId, credit.AccountId] += debit.Amount;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Returns a snapshot of all transactions as a ReadOnlySpan.
        /// </summary>
        public ReadOnlySpan<Transaction> Snapshot()
            => new ReadOnlySpan<Transaction>(Transactions.ToArray());
    }
}