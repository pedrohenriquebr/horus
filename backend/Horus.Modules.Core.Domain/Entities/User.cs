namespace Horus.Modules.Core.Domain.Entities;

public class User : BaseEntityWithMetadata
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string Name { get; private set; }
    public string PasswordHash { get; private set; }
    public string Salt { get; private set; }
    
    // for ef core
    public User(Guid id, string email, string name, string passwordHash, string salt)
    {
        Id = id;
        Email = email;
        Name = name;
        PasswordHash = passwordHash;
        Salt = salt;
    }

    public void UpdateEmail(string newEmail)
    {
        Email = newEmail;
    }

    public void UpdateName(string name)
    {
        Name = name;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void UpdateSalt(string salt)
    {
        Salt = salt;
    }
}