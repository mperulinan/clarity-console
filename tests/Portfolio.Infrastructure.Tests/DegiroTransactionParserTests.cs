using System.Text;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ImportParsers;

namespace Portfolio.Infrastructure.Tests;

public class DegiroTransactionParserTests
{
    private readonly DegiroTransactionParser _sut = new();

    [Fact]
    public async Task ParseAsync_ValidSpanishCsvWithBuyAndSell_ReturnsCorrectImportedRows()
    {
        // Arrange
        var csv = """
            Fecha,Hora,Producto,ISIN,Bolsa de referencia,Centro de ejecución,Número,Precio,,Valor local,,Valor EUR,Tipo de cambio,Comisión AutoFX,Costes de transacción y/o externos EUR,Total EUR,ID Orden,
            20-10-2023,14:30,ACME CORP,XX0000000001,TST,XTST,10,175.50,USD,-1755.00,USD,-1650.25,1.0635,-1.20,-1.00,-1652.45,aaaaaaaa-bbbb-cccc-dddd-000000000001,
            21-10-2023,15:45,ACME CORP,XX0000000001,TST,XTST,-5,180.00,USD,900.00,USD,850.00,1.0588,-0.80,-1.00,848.20,aaaaaaaa-bbbb-cccc-dddd-000000000002,
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var buy = result[0];
        Assert.Equal(new DateTime(2023, 10, 20, 14, 30, 0, DateTimeKind.Utc), buy.Date);
        Assert.Equal("ACME CORP", buy.ProductName);
        Assert.Equal("XX0000000001", buy.Isin);
        Assert.Equal("TST", buy.Exchange);
        Assert.Equal(10m, buy.Quantity);
        Assert.Equal(175.50m, buy.PricePerUnit);
        Assert.Equal("USD", buy.PriceCurrency);
        Assert.Equal("EUR", buy.TotalCurrency);
        Assert.Equal(1.0635m, buy.FxRate);
        Assert.Equal(2.20m, buy.FeeEur);
        Assert.Equal(-1652.45m, buy.TotalAmount);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-000000000001", buy.BrokerTransactionId);

        var sell = result[1];
        Assert.Equal(new DateTime(2023, 10, 21, 15, 45, 0, DateTimeKind.Utc), sell.Date);
        Assert.Equal(-5m, sell.Quantity);
        Assert.Equal(180.00m, sell.PricePerUnit);
        Assert.Equal("USD", sell.PriceCurrency);
        Assert.Equal(1.80m, sell.FeeEur);
        Assert.Equal(848.20m, sell.TotalAmount);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-000000000002", sell.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_EurTradeWithNullFxRateAndFee_ParsesSuccessfully()
    {
        // Arrange
        var csv = """
            Fecha,Hora,Producto,ISIN,Bolsa de referencia,Centro de ejecución,Número,Precio,,Valor local,,Valor EUR,Tipo de cambio,Comisión AutoFX,Costes de transacción y/o externos EUR,Total EUR,ID Orden,
            22-10-2023,10:00,GLOBEX HOLDINGS,XX9999999002,TST,XTST,5,120.00,EUR,-600.00,EUR,-600.00,,,,-600.00,cccccccc-dddd-eeee-ffff-000000000001,
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.Single(result);
        var row = result[0];
        Assert.Equal("XX9999999002", row.Isin);
        Assert.Equal(5m, row.Quantity);
        Assert.Equal(120.00m, row.PricePerUnit);
        Assert.Equal("EUR", row.PriceCurrency);
        Assert.Null(row.FxRate);
        Assert.Null(row.FeeEur);
        Assert.Equal(-600.00m, row.TotalAmount);
        Assert.Equal("cccccccc-dddd-eeee-ffff-000000000001", row.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_InvalidHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var csv = """
            Date,Time,Product,ISIN,Reference exchange,Venue,Quantity,Price
            20-10-2023,14:30,ACME CORP,XX0000000001,TST,XTST,10,175.50
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ParseAsync(stream));
    }

    [Fact]
    public async Task ParseAsync_OldFormatWithShiftedOrderColumnAndSpanishDecimals_ParsesCorrectly()
    {
        // Arrange
        // Older DEGIRO exports had a trailing ",," after "Total EUR" in the header,
        // which pushed the Order ID value one column to the right of the "ID Orden" header.
        // The parser's ResolveBrokerTransactionId() falls back to the adjacent column in that case.
        var csv = """
            Fecha,Hora,Producto,ISIN,Bolsa de referencia,Centro de ejecución,Número,Precio,,Valor local,,Valor EUR,Tipo de cambio,Comisión AutoFX,Costes de transacción y/o externos EUR,Total EUR,ID Orden,
            15-03-2022,10:30,ACME CORP,XX0000000001,TST,XTST,5,"100,0000",USD,"-500,00",USD,"-460,20","1,0864","-1,15","-1,00","-462,35",,aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var row = result[0];
        Assert.Equal(new DateTime(2022, 3, 15, 10, 30, 0, DateTimeKind.Utc), row.Date);
        Assert.Equal("ACME CORP", row.ProductName);
        Assert.Equal("XX0000000001", row.Isin);
        Assert.Equal("TST", row.Exchange);
        Assert.Equal(5m, row.Quantity);
        Assert.Equal(100.0000m, row.PricePerUnit);
        Assert.Equal("USD", row.PriceCurrency);
        Assert.Equal(1.0864m, row.FxRate);
        Assert.Equal(2.15m, row.FeeEur);
        Assert.Equal(-462.35m, row.TotalAmount);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", row.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_NewFormatWithoutTrailingShift_ParsesCorrectly()
    {
        // Arrange
        // Current DEGIRO exports no longer have the trailing ",," after "Total EUR";
        // the Order ID sits directly under its "ID Orden" header column.
        var csv = """
            Fecha,Hora,Producto,ISIN,Bolsa de referencia,Centro de ejecución,Número,Precio,,Valor local,,Valor EUR,Tipo de cambio,Comisión AutoFX,Costes de transacción y/o externos EUR,Total EUR,ID Orden
            15-03-2022,10:30,ACME CORP,XX0000000001,TST,XTST,5,"100,0000",USD,"-500,00",USD,"-460,20","1,0864","-1,15","-1,00","-462,35",aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
            20-07-2020,16:00,GLOBEX HOLDINGS ADR,XX9999999002,NSY,XNYS,8,"55,2500",USD,"-442,00",USD,"-390,15","1,1328","-0,98","-0,50","-391,63",bbbbbbbb-cccc-dddd-eeee-ffffffffffff
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var first = result[0];
        Assert.Equal("ACME CORP", first.ProductName);
        Assert.Equal("XX0000000001", first.Isin);
        Assert.Equal(5m, first.Quantity);
        Assert.Equal(100.0000m, first.PricePerUnit);
        Assert.Equal("USD", first.PriceCurrency);
        Assert.Equal(-462.35m, first.TotalAmount);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", first.BrokerTransactionId);

        var second = result[1];
        Assert.Equal("GLOBEX HOLDINGS ADR", second.ProductName);
        Assert.Equal("XX9999999002", second.Isin);
        Assert.Equal(8m, second.Quantity);
        Assert.Equal(55.2500m, second.PricePerUnit);
        Assert.Equal("USD", second.PriceCurrency);
        Assert.Equal(-391.63m, second.TotalAmount);
        Assert.Equal(1.48m, second.FeeEur);
        Assert.Equal("bbbbbbbb-cccc-dddd-eeee-ffffffffffff", second.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_EmptyCsv_ReturnsEmptyList()
    {
        // Arrange
        var csv = "";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseAsync_AccountStatementCsv_ParsesCashRowsAndSkipsTrades()
    {
        // Arrange
        const string csv = """
            Fecha,Hora,Fecha valor,Producto,ISIN,Descripción,Tipo,Variación,,Saldo,,ID Orden
            25-05-2021,09:00,25-05-2021,,,flatex Deposit,,EUR,"2300,00",EUR,"2300,00",
            26-05-2021,21:50,26-05-2021,ACME CORP,XX0000000001,"Compra 5 Acme Corp@200 USD (XX0000000001)",,USD,"-1000,00",USD,"-1000,00",ord-1
            27-05-2021,10:00,27-05-2021,,,Degiro Cash Sweep Transfer,,EUR,"100,00",EUR,"2400,00",
            06-02-2022,09:59,31-01-2022,,,Comisión de conectividad con el mercado 2022 (New York Stock Exchange - NSY),,EUR,"-2,50",EUR,"2297,50",
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(TransactionType.Deposit, result[0].TransactionType);
        Assert.Equal(2300m, result[0].TotalAmount);
        Assert.Equal(TransactionType.Loss, result[1].TransactionType);
        Assert.Equal(-2.50m, result[1].TotalAmount);
    }

    [Fact]
    public async Task ParseAsync_AccountStatementWithWithdrawalsDividendsAndFees_ParsesCorrectly()
    {
        // Arrange
        const string csv = """
            Fecha,Hora,Fecha valor,Producto,ISIN,Descripción,Tipo,Variación,,Saldo,,ID Orden
            07-04-2024,17:50,08-04-2024,,,flatex Withdrawal,,EUR,"-5000,00",EUR,"-4900,00",
            14-07-2024,08:08,13-07-2024,ACME CORP ADR,XX0000000001,Dividendo,,USD,"25,00",USD,"25,00",
            14-07-2024,08:09,13-07-2024,ACME CORP ADR,XX0000000001,ADR/GDR Pass-Through Fee,,USD,"-0,50",USD,"24,50",
            03-10-2024,11:05,30-09-2024,,,Interés,,EUR,"-0,03",EUR,"1500,00",
            04-10-2024,06:21,30-09-2024,,,Flatex Interest Income,,EUR,"0,00",EUR,"1500,00",
            07-04-2024,17:50,08-04-2024,,,Processed Flatex Withdrawal,,EUR,"5000,00",EUR,"100,00",
            15-07-2024,07:42,15-07-2024,,,"Transferir a su Cuenta de Efectivo en flatexDEGIRO Bank: 11,7 EUR",,,,EUR,"100,00",
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.Equal(4, result.Count);

        var withdrawal = result[0];
        Assert.Equal(TransactionType.Withdrawal, withdrawal.TransactionType);
        Assert.Equal(-5000m, withdrawal.TotalAmount);
        Assert.Equal("EUR", withdrawal.TotalCurrency);
        Assert.Equal(5000m, withdrawal.Quantity);

        var dividend = result[1];
        Assert.Equal(TransactionType.Reward, dividend.TransactionType);
        Assert.Equal(25m, dividend.TotalAmount);
        Assert.Equal("USD", dividend.PriceCurrency);
        Assert.Equal("USD", dividend.TotalCurrency);
        Assert.Equal("XX0000000001", dividend.Isin);

        var adrFee = result[2];
        Assert.Equal(TransactionType.Loss, adrFee.TransactionType);
        Assert.Equal(-0.50m, adrFee.TotalAmount);
        Assert.Equal("USD", adrFee.PriceCurrency);
        Assert.Equal("USD", adrFee.TotalCurrency);
        Assert.Equal("XX0000000001", adrFee.Isin);

        var interest = result[3];
        Assert.Equal(TransactionType.Loss, interest.TransactionType);
        Assert.Equal(-0.03m, interest.TotalAmount);
        Assert.Equal("EUR", interest.TotalCurrency);
    }
}
