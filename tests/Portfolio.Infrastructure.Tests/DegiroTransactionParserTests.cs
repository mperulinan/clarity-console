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
