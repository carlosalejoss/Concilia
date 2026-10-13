namespace Concilia.Runner.Security;

// Replaces the gateway key and address with "***". Every line coming from the
// child process goes through Redact before it is logged, stored or sent to the web.
public class SecretRedactor
{
    public const string Mask = "***";

    private readonly List<string> _secrets;

    public SecretRedactor(IEnumerable<string?> secrets)
    {
        // Longest first, so a full URL is masked before the host inside it.
        _secrets = secrets
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(s => s.Length)
            .ToList();
    }

    // The secrets of a gateway: the key, the base URL and its host name.
    // The host is masked on its own because it can appear in error messages
    // without the rest of the URL.
    public static SecretRedactor ForGateway(GatewaySettings? gateway)
    {
        if (gateway is null)
        {
            return new SecretRedactor([]);
        }

        var secrets = new List<string?> { gateway.AuthToken, gateway.BaseUrl, gateway.BaseUrl.TrimEnd('/') };
        if (Uri.TryCreate(gateway.BaseUrl, UriKind.Absolute, out var uri))
        {
            secrets.Add(uri.Authority); // host plus port, if any
            secrets.Add(uri.Host);
        }

        // In JSON text "/" may be written as "\/".
        secrets.AddRange(secrets.Where(s => s is not null && s.Contains('/')).Select(s => s!.Replace("/", "\\/")).ToList());

        return new SecretRedactor(secrets);
    }

    public string Redact(string text)
    {
        foreach (var secret in _secrets)
        {
            // Host names are case-insensitive, so the match is too.
            text = text.Replace(secret, Mask, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }
}
