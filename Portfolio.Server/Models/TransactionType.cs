using System;
using System.Collections.Generic;

namespace Portfolio.Server.Models;

public partial class TransactionType
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
