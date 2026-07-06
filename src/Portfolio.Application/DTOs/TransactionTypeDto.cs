using System;

namespace Portfolio.Application.DTOs;

public class TransactionTypeDto
{
    public string Value { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool RequiresFromAsset { get; set; }
    public bool RequiresToAsset { get; set; }
    public bool RequiresSpotPrice { get; set; }
}
