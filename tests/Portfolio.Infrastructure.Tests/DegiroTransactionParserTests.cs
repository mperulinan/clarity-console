using System.Text;
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
            20-10-2023,14:30,APPLE INC,US0378331005,NSY,XNYS,10,175.50,USD,-1755.00,USD,-1650.25,1.0635,-1.20,-1.00,-1652.45,06db189d-3f4c-479f-b4d4-9d2e93dc0f2f,
            21-10-2023,15:45,APPLE INC,US0378331005,NSY,XNYS,-5,180.00,USD,900.00,USD,850.00,1.0588,-0.80,-1.00,848.20,17ec291a-4e2b-482f-8a12-7f9e8a1a3b5c,
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var buy = result[0];
        Assert.Equal(new DateTime(2023, 10, 20, 14, 30, 0, DateTimeKind.Utc), buy.Date);
        Assert.Equal("APPLE INC", buy.ProductName);
        Assert.Equal("US0378331005", buy.Isin);
        Assert.Equal("NSY", buy.Exchange);
        Assert.Equal(10m, buy.Quantity);
        Assert.Equal(175.50m, buy.PricePerUnit);
        Assert.Equal("USD", buy.PriceCurrency);
        Assert.Equal(1.0635m, buy.FxRate);
        Assert.Equal(1.00m, buy.FeeEur);
        Assert.Equal(-1652.45m, buy.TotalEur);
        Assert.Equal("06db189d-3f4c-479f-b4d4-9d2e93dc0f2f", buy.BrokerTransactionId);

        var sell = result[1];
        Assert.Equal(new DateTime(2023, 10, 21, 15, 45, 0, DateTimeKind.Utc), sell.Date);
        Assert.Equal(-5m, sell.Quantity);
        Assert.Equal(180.00m, sell.PricePerUnit);
        Assert.Equal("USD", sell.PriceCurrency);
        Assert.Equal(848.20m, sell.TotalEur);
        Assert.Equal("17ec291a-4e2b-482f-8a12-7f9e8a1a3b5c", sell.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_EurTradeWithNullFxRateAndFee_ParsesSuccessfully()
    {
        // Arrange
        var csv = """
            Fecha,Hora,Producto,ISIN,Bolsa de referencia,Centro de ejecución,Número,Precio,,Valor local,,Valor EUR,Tipo de cambio,Comisión AutoFX,Costes de transacción y/o externos EUR,Total EUR,ID Orden,
            22-10-2023,10:00,SAP SE,DE0007164600,XET,XETR,5,120.00,EUR,-600.00,EUR,-600.00,,,,-600.00,28fc302b-5f3c-493f-9b23-8a239b2b4c6d,
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        // Act
        var result = await _sut.ParseAsync(stream);

        // Assert
        Assert.Single(result);
        var row = result[0];
        Assert.Equal("DE0007164600", row.Isin);
        Assert.Equal(5m, row.Quantity);
        Assert.Equal(120.00m, row.PricePerUnit);
        Assert.Equal("EUR", row.PriceCurrency);
        Assert.Null(row.FxRate);
        Assert.Null(row.FeeEur);
        Assert.Equal(-600.00m, row.TotalEur);
        Assert.Equal("28fc302b-5f3c-493f-9b23-8a239b2b4c6d", row.BrokerTransactionId);
    }

    [Fact]
    public async Task ParseAsync_InvalidHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var csv = """
            Date,Time,Product,ISIN,Reference exchange,Venue,Quantity,Price
            20-10-2023,14:30,APPLE INC,US0378331005,NSY,XNYS,10,175.50
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
        Assert.Equal(1.00m, row.FeeEur);
        Assert.Equal(-462.35m, row.TotalEur);
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
        Assert.Equal(-462.35m, first.TotalEur);
        Assert.Equal("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", first.BrokerTransactionId);

        var second = result[1];
        Assert.Equal("GLOBEX HOLDINGS ADR", second.ProductName);
        Assert.Equal("XX9999999002", second.Isin);
        Assert.Equal(8m, second.Quantity);
        Assert.Equal(55.2500m, second.PricePerUnit);
        Assert.Equal("USD", second.PriceCurrency);
        Assert.Equal(-391.63m, second.TotalEur);
        Assert.Equal(0.50m, second.FeeEur);
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
}
