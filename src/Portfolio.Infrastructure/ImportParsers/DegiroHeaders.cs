namespace Portfolio.Infrastructure.ImportParsers;

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
}

