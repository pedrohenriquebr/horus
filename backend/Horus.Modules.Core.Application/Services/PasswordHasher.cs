using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Horus.Modules.Core.Application.Services;

public interface IPasswordHasher
{
    (string Hash, string Salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);
}

public class PasswordHasher : IPasswordHasher
{
    
    private readonly IOptions<SecurityOptions> _securityOptions;

    public PasswordHasher(IOptions<SecurityOptions> securityOptions)
    {
        _securityOptions = securityOptions;
    }

    public  (string Hash, string Salt) HashPassword(string password)
    {
        // Generate a unique 16-byte salt for the user
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        
        byte[] pepper = Encoding.UTF8.GetBytes(_securityOptions.Value.PepperSecret);
        
        // Combine password + salt + pepper into a single byte array
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] combined = new byte[passwordBytes.Length + salt.Length+pepper.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        Buffer.BlockCopy(pepper, 0, combined, passwordBytes.Length + salt.Length, pepper.Length);
        
        // Configure Argon2
        var argon2 = GenerateArgon2Id(combined, salt);
        
        // Generate the hash
        byte[] hash = argon2.GetBytes(32); // 32-byte hash

        // Convert to strings for storage
        string hashString = Convert.ToBase64String(hash);
        string saltString = Convert.ToBase64String(salt);

        return (hashString, saltString);
    }

    private Argon2id GenerateArgon2Id(byte[] combined, byte[] salt)
    {
        return new Argon2id(combined)
        {
            Salt = salt,       // Still include salt in Argon2's salt parameter
            DegreeOfParallelism = _securityOptions.Value.Argon2Options.DegreeOfParallelism,    // Number of threads
            MemorySize = _securityOptions.Value.Argon2Options.MemorySize,         // 64MB memory cost
            Iterations = _securityOptions.Value.Argon2Options.Iterations              // Number of passes
        };
    }

    public bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
    {
        // Decode stored salt and hash
        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] expectedHash = Convert.FromBase64String(storedHash);
        byte[] pepper = Encoding.UTF8.GetBytes(_securityOptions.Value.PepperSecret);


        // Reconstruct the combined input (password + salt + pepper)
        byte[] passwordBytes = Encoding.UTF8.GetBytes(enteredPassword);
        byte[] combined = new byte[passwordBytes.Length + salt.Length + pepper.Length];
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);
        Buffer.BlockCopy(pepper, 0, combined, passwordBytes.Length + salt.Length, pepper.Length);

        // Recompute the hash with the same parameters
        var argon2 = GenerateArgon2Id(combined, salt);

        byte[] actualHash = argon2.GetBytes(32);

        // Compare hashes (constant-time to prevent timing attacks)
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}