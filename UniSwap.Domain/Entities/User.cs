using UniSwap.Domain.ValueObjects;

namespace UniSwap.Domain.Entities;

public enum UserRole { User, Admin }

public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    private User() { Name = null!; Email = null!; }

    public User(string name, Email email, UserRole role = UserRole.User)
    {
        Id = Guid.NewGuid();
        Name = ValidateName(name);
        Email = email;
        Role = role;
    }

    public void UpdateName(string name) => Name = ValidateName(name);

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 200) throw new ArgumentException("Name is too long.", nameof(name));
        return name.Trim();
    }

    public void SetPasswordHash(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        PasswordHash = hash;
    }
}
