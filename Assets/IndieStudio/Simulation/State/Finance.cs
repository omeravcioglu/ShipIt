using System;
using System.Collections.Generic;
using System.Linq;

namespace IndieStudio.Simulation
{
    public enum TxCategory
    {
        Rent,
        Living,
        Utilities,
        Software,
        Hardware,
        Assets,
        Marketing,
        StoreFees,
        Education,
        Freelance,
        GameSales,
        DayJob,
        Family,
        Interest,
        Taxes,
        Other,
    }

    public sealed class Transaction
    {
        public int Day;
        public double Amount;
        public TxCategory Category;
        public string Description;
    }

    public sealed class RecurringCost
    {
        public string Id;
        public string Name;
        public double MonthlyAmount;
        public TxCategory Category;
    }

    /// <summary>Money that is owed to you but not paid yet (platform payouts, client invoices).</summary>
    public sealed class ScheduledPayment
    {
        public int Day;
        public double Amount;
        public TxCategory Category;
        public string Description;
    }

    public sealed class MonthlyReport
    {
        public int MonthIndex;
        public double Income;
        public double Expenses;
        public double CashAtClose;
        public readonly Dictionary<TxCategory, double> ByCategory = new Dictionary<TxCategory, double>();
        public double Net => Income + Expenses;
    }

    public sealed class Finance
    {
        public double Cash;
        public double CreditLimit = Tuning.CreditLimit;
        public readonly List<Transaction> Transactions = new List<Transaction>();
        public readonly List<RecurringCost> Recurring = new List<RecurringCost>();
        public readonly List<ScheduledPayment> Scheduled = new List<ScheduledPayment>();
        public readonly List<MonthlyReport> Reports = new List<MonthlyReport>();
        public bool EverNegative;

        public double MonthlyRecurringTotal => Recurring.Sum(r => r.MonthlyAmount);
        public double PendingIncome => Scheduled.Sum(p => p.Amount);

        public Transaction Apply(int day, double amount, TxCategory category, string description)
        {
            amount = Math.Round(amount, 2);
            Cash = Math.Round(Cash + amount, 2);
            var tx = new Transaction { Day = day, Amount = amount, Category = category, Description = description };
            Transactions.Add(tx);
            if (Cash < 0) EverNegative = true;
            return tx;
        }

        public RecurringCost GetRecurring(string id) => Recurring.FirstOrDefault(r => r.Id == id);

        public void SetRecurring(string id, string name, double monthly, TxCategory category)
        {
            var existing = GetRecurring(id);
            if (existing == null) Recurring.Add(new RecurringCost { Id = id, Name = name, MonthlyAmount = monthly, Category = category });
            else
            {
                existing.Name = name;
                existing.MonthlyAmount = monthly;
            }
        }

        public void RemoveRecurring(string id) => Recurring.RemoveAll(r => r.Id == id);

        /// <summary>Days until cash runs out at the current monthly costs, ignoring money not yet received.</summary>
        public double RunwayDays()
        {
            double monthly = MonthlyRecurringTotal;
            if (Cash <= 0) return 0;
            if (monthly <= 0) return 9999;
            return Cash / monthly * 30.0;
        }
    }
}
