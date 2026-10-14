using System.Globalization;
using MimeKit;
using MsgReader.Outlook;

namespace Concilia.Core.Files;

// An email (.eml, the standard format, or .msg, Outlook's) as text: the
// subject as a heading, who sends it, to whom and when as list items, the
// body and the names of the attachments. The attachments are not read: to
// use one, upload it as a file of its own.
internal static class EmailText
{
    public static string FromEml(byte[] content)
    {
        using var stream = new MemoryStream(content);
        var message = MimeMessage.Load(stream);
        var body = message.TextBody ?? (message.HtmlBody is { } html ? HtmlText.Convert(html) : "");
        var attachments = message.Attachments.Select(a => a is MimePart part ? part.FileName : a.ContentType.Name);
        return Email(
            message.Subject,
            message.From.ToString(),
            message.To.ToString(),
            message.Cc.ToString(),
            message.Date == DateTimeOffset.MinValue ? null : message.Date,
            body,
            attachments);
    }

    public static string FromMsg(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var message = new Storage.Message(stream);
        var body = message.BodyText ?? (message.BodyHtml is { } html ? HtmlText.Convert(html) : "");
        // An attachment can be a file or another email.
        var attachments = message.Attachments.Select(a => a switch
        {
            Storage.Attachment file => file.FileName,
            Storage.Message email => email.Subject,
            _ => null,
        });
        return Email(
            message.Subject,
            message.Sender is { } sender ? Person(sender.DisplayName, sender.Email) : "",
            Recipients(message, RecipientType.To),
            Recipients(message, RecipientType.Cc),
            message.SentOn,
            body,
            attachments);
    }

    private static string Recipients(Storage.Message message, RecipientType type) =>
        string.Join(", ", message.Recipients.Where(r => r.Type == type).Select(r => Person(r.DisplayName, r.Email)));

    // "Name <address>", or whichever of the two there is.
    private static string Person(string? name, string? address) =>
        (string.IsNullOrWhiteSpace(name), string.IsNullOrWhiteSpace(address)) switch
        {
            (false, false) when name != address => $"{name} <{address}>",
            (false, _) => name!,
            (true, false) => address!,
            _ => "",
        };

    private static string Email(string? subject, string from, string to, string cc, DateTimeOffset? date,
        string body, IEnumerable<string?> attachments)
    {
        var blocks = new List<string>
        {
            $"# {(string.IsNullOrWhiteSpace(subject) ? "(Sin asunto)" : MarkdownText.OneLine(subject))}",
        };
        if (from.Length > 0)
            blocks.Add($"- De: {from}");
        if (to.Length > 0)
            blocks.Add($"- Para: {to}");
        if (cc.Length > 0)
            blocks.Add($"- CC: {cc}");
        if (date is { } sent)
            blocks.Add($"- Fecha: {sent.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}");
        blocks.Add(body.Trim());
        blocks.AddRange(attachments.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => $"- Adjunto: {name}"));
        return MarkdownText.Blocks(blocks);
    }
}
