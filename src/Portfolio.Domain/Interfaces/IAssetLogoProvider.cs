namespace Portfolio.Domain.Interfaces;

public interface IAssetLogoProvider
{
    Task<string?> GetLogoUrlAsync(string symbol);
}
