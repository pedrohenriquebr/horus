namespace Horus.Modules.Shared.Contracts.Services;

public interface IEmailProvider
{
    void SendEmail(string to, string subject, string body);
}