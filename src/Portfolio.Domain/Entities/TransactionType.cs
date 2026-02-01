namespace Portfolio.Domain.Entities;

public class TransactionType
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    // Constructor for EF Core.
    private TransactionType() { }

    public TransactionType(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }
        
        Code = code;
        Name = name;
    }
}
