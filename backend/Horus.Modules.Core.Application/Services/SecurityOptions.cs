namespace Horus.Modules.Core.Application.Services;

public class SecurityOptions
{
    public string PepperSecret { get; set; }
    public Argon2Options Argon2Options { get; set; }
}

public class Argon2Options
{
    public int DegreeOfParallelism { get; set; }
    public int MemorySize { get; set; }
    public int Iterations { get; set; }
}