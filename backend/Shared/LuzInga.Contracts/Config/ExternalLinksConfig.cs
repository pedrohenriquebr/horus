namespace Horus.Modules.Shared.Contracts.Config;

public sealed class ExternalLinksConfig
{
    public Uri WebAppHostName { get; set; }
    public NewsletterLinks Newsletter { get; set; }
}

public sealed class NewsletterLinks
{
    public string ConfirmEmail { get; set; }
    public string Manage { get; set; }
    public string RefreshConfirmationCode { get; set; }
}