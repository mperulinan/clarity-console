namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// A keyword used to match DEGIRO account statement description fields,
/// paired with whether it must appear at the start or anywhere in the string.
/// </summary>
internal sealed record DescriptionKeyword(string Value, bool StartsWith = false)
{
    public bool Matches(string description) =>
        StartsWith
            ? description.StartsWith(Value, StringComparison.OrdinalIgnoreCase)
            : description.Contains(Value, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Column header constants and required field definitions for DEGIRO (Spanish) CSV exports.
/// </summary>
internal static class DegiroHeaders
{
    public const string Date = "Fecha";
    public const string Time = "Hora";
    public const string Product = "Producto";
    public const string Isin = "ISIN";
    public const string ReferenceExchange = "Bolsa de referencia";
    public const string ExecutionVenue = "Centro de ejecución";
    public const string Quantity = "Número";
    public const string Price = "Precio";
    public const string LocalValue = "Valor local";
    public const string ValueEur = "Valor EUR";
    public const string FxRate = "Tipo de cambio";
    public const string AutoFxCommission = "Comisión AutoFX";
    public const string TransactionAndExternalCostsEur = "Costes de transacción y/o externos EUR";
    public const string TotalEur = "Total EUR";
    public const string OrderId = "ID Orden";

    // Account statement (Estado de cuenta) headers
    public const string ValueDate = "Fecha valor";
    public const string Description = "Descripción";
    public const string Type = "Tipo";
    public const string Variation = "Variación";
    public const string Balance = "Saldo";

    public const string DefaultCurrency = "EUR";

    /// <summary>
    /// Description substrings that identify internal cash-sweep or hold rows to skip entirely.
    /// </summary>
    public static readonly DescriptionKeyword[] InternalSweepKeywords =
    [
        new("Cash Sweep"),
        new("Transferir a",                  StartsWith: true),
        new("Transferir desde",              StartsWith: true),
        new("Processed Flatex Withdrawal",   StartsWith: true)
    ];

    /// <summary>
    /// Description prefixes/substrings that identify trade-execution rows to skip when an order ID is present.
    /// </summary>
    public static readonly DescriptionKeyword[] TradeRelatedKeywords =
    [
        new("Compra",                StartsWith: true),
        new("Venta",                 StartsWith: true),
        new("Buy",                   StartsWith: true),
        new("Sell",                  StartsWith: true),
        new("Costes de transacción"),
        new("Cambio de Divisa")
    ];

    // ── TransactionType resolution keywords ──────────────────────────────────

    public static readonly DescriptionKeyword[] DepositKeywords =
    [
        new("Deposit"),
        new("Depósito"),
        new("Ingreso")
    ];

    public static readonly DescriptionKeyword[] WithdrawalKeywords =
    [
        new("Withdrawal"),
        new("Retirada")
    ];

    public static readonly DescriptionKeyword[] RewardKeywords =
    [
        new("Dividendo",              StartsWith: true),
        new("Dividend",               StartsWith: true),
        new("Rendimiento de capital")
    ];

    public static readonly DescriptionKeyword[] LossKeywords =
    [
        new("conectividad"),
        new("connectivity"),
        new("Pass-Through Fee"),
        new("Retención del dividendo")
    ];

    public static readonly DescriptionKeyword[] InterestKeywords =
    [
        new("Interés"),
        new("Interest")
    ];

    /// <summary>
    /// Mandatory headers required to validate that an uploaded CSV is a recognized DEGIRO Spanish export.
    /// </summary>
    public static readonly string[] Required =
    [
        Date,
        Product,
        Isin,
        Quantity,
        Price,
        TotalEur,
        OrderId
    ];

    /// <summary>
    /// Mandatory headers required to validate that an uploaded CSV is a recognized DEGIRO Spanish account statement.
    /// </summary>
    public static readonly string[] AccountRequired =
    [
        Date,
        Time,
        Description,
        Variation,
        Balance
    ];
}

