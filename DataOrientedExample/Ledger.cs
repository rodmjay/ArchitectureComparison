﻿using System.Runtime.InteropServices;

namespace DataOrientedExample;

public class Ledger
{
    public List<Account> Accounts = new List<Account>();
    public List<Entry> Entries = new List<Entry>();
    public List<Transaction> Transactions = new List<Transaction>();
    private int nextTranId = 1;

    // Add a new account and return its Id
    public int AddAccount(string name, AccountType type)
    {
        var id = Accounts.Count;
        Accounts.Add(new Account { Id = id, Name = name, Type = type });
        return id;
    }

    /// <summary>
    /// Recalculates the balance for a given account by summing all entries in a sequential (non‑parallel) manner.
    /// </summary>
    /// <param name="accountId">The ID of the account to recalculate.</param>
    /// <returns>The computed balance (sum of amounts) for the account.</returns>
    public decimal RecalculateAccountBalanceSequential(int accountId)
    {
        decimal total = 0;
        foreach (var entry in Entries)
        {
            if (entry.AccountId == accountId)
            {
                total += entry.Amount;
            }
        }
        return total;
    }



    /// <summary>
    /// Recalculates the balance for a given account by summing
    /// all entries (debits and credits) in parallel.
    /// This method uses a fork-and-join pattern with Parallel.ForEach.
    /// </summary>
    /// <param name="accountId">The ID of the account to recalculate.</param>
    /// <returns>The resulting balance (sum of amounts) for the account.</returns>
    public decimal RecalculateAccountBalanceParallel(int accountId)
    {
        decimal totalBalance = 0;
        // A lock object for safely accumulating into the total.
        object lockObj = new object();

        // Use Parallel.ForEach with thread-local accumulators.
        Parallel.ForEach(
            source: Entries,
            // Initialize each thread's local sum to zero.
            localInit: () => 0m,
            // For each entry, if it belongs to the specified account, add its amount.
            body: (entry, loopState, localSum) =>
            {
                if (entry.AccountId == accountId)
                {
                    localSum += entry.Amount;
                }
                return localSum;
            },
            // Safely add each thread's local sum to the overall total.
            localFinally: localSum =>
            {
                lock (lockObj)
                {
                    totalBalance += localSum;
                }
            }
        );

        return totalBalance;
    }

    
    
    // Post a transaction with one debit and one credit entry
    public int PostTransaction(DateTime date, string desc,
        int debitAccount, decimal debitAmount,
        int creditAccount, decimal creditAmount)
    {
        if (debitAmount <= 0 || creditAmount <= 0)
            throw new ArgumentException("Amounts must be positive.");
        if (debitAmount != creditAmount)
            throw new InvalidOperationException("Debit and credit must balance.");

        var tranId = (int)nextTranId++;
        Transactions.Add(new Transaction { Id = tranId, Date = date, Description = desc });
        // Append debit entry (positive amount for debit, meaning asset/expense increase)
        Entries.Add(new Entry { TransactionId = tranId, AccountId = debitAccount, Amount = debitAmount });
        // Append credit entry (negative amount for credit, or we can use positive and interpret by account type)
        Entries.Add(new Entry { TransactionId = tranId, AccountId = creditAccount, Amount = -creditAmount });
        return tranId;
    }

    public ReadOnlySpan<Transaction> Snapshot()
    {    
        // This allocates a new array (with a copy of the list's data)
        return new ReadOnlySpan<Transaction>(Transactions.ToArray());
    }
}