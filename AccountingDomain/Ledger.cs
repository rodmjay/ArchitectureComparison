namespace AccountingDomain;

public class Ledger
{
    public List<Account> Accounts = new();
    public List<Entry> Entries = new();
    public List<Transaction> Transactions = new();
    private int nextTranId = 1;

    /// <summary>
    /// Searches the ledger’s transactions based on a provided predicate and returns a read-only span
    /// of the transactions that match. This method is synchronous and does all processing in memory.
    /// </summary>
    /// <param name="predicate">An expression to filter transactions.</param>
    /// <returns>
    /// A ReadOnlySpan over an array of Transaction objects that match the predicate.
    /// Note that the returned span is backed by a new array allocated in this method.
    /// </returns>
    public ReadOnlySpan<Transaction> SearchTransactions(Func<Transaction, bool> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        // Filter the in-memory list.
        Transaction[] matches = Transactions.Where(predicate).ToArray();

        // Return a ReadOnlySpan over the resulting array.
        return new ReadOnlySpan<Transaction>(matches);
    }

    /// <summary>
    /// Alternative search that returns a copy of the matching transactions as a List.
    /// </summary>
    public List<Transaction> SearchTransactionsCopy(Func<Transaction, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // Filter transactions into a list.
        return Transactions.Where(predicate).ToList();
    }

    // Add a new account and return its Id
    public int AddAccount(string name, AccountType type)
    {
        var id = Accounts.Count;
        Accounts.Add(new Account { Id = id, Name = name, Type = type });
        return id;
    }

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

        var tranId = nextTranId++;
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