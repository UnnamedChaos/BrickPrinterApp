using BrickPrinterApp.Interfaces;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;

namespace BrickPrinterApp.Services;

/// <summary>
/// Service for retrieving emails from Gmail using Google API
/// </summary>
public class GmailService : IGmailService
{
    private readonly GoogleAuthService _authService;
    private Google.Apis.Gmail.v1.GmailService? _gmailApi;

    public GmailService(GoogleAuthService authService)
    {
        _authService = authService;
    }

    /// <inheritdoc />
    public bool IsAvailable => _authService.Status == GoogleAuthStatus.Connected;

    /// <inheritdoc />
    public async Task<List<EmailSummary>> GetEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default)
    {
        return await GetEmailsInternalAsync(maxResults, query: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<EmailSummary>> GetUnreadEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default)
    {
        return await GetEmailsInternalAsync(maxResults, query: "is:unread", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var api = GetGmailApi();
        if (api == null)
            return 0;

        try
        {
            var request = api.Users.Labels.Get("me", "UNREAD");
            var label = await request.ExecuteAsync(cancellationToken);
            return label.MessagesUnread ?? 0;
        }
        catch
        {
            // Fallback: count unread messages in INBOX
            try
            {
                var inboxRequest = api.Users.Labels.Get("me", "INBOX");
                var inbox = await inboxRequest.ExecuteAsync(cancellationToken);
                return inbox.MessagesUnread ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    private async Task<List<EmailSummary>> GetEmailsInternalAsync(int maxResults, string? query, CancellationToken cancellationToken)
    {
        var result = new List<EmailSummary>();

        var api = GetGmailApi();
        if (api == null)
            return result;

        try
        {
            // List messages from inbox
            var listRequest = api.Users.Messages.List("me");
            listRequest.MaxResults = maxResults;
            listRequest.LabelIds = "INBOX";

            if (!string.IsNullOrEmpty(query))
            {
                listRequest.Q = query;
            }

            var listResponse = await listRequest.ExecuteAsync(cancellationToken);

            if (listResponse.Messages == null || listResponse.Messages.Count == 0)
                return result;

            // Fetch details for each message
            foreach (var messageRef in listResponse.Messages)
            {
                try
                {
                    var getRequest = api.Users.Messages.Get("me", messageRef.Id);
                    getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Metadata;
                    getRequest.MetadataHeaders = new[] { "From", "Subject", "Date" };

                    var message = await getRequest.ExecuteAsync(cancellationToken);
                    var emailSummary = ParseMessage(message);
                    result.Add(emailSummary);
                }
                catch
                {
                    // Skip messages that fail to load
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return result;
    }

    private EmailSummary ParseMessage(Google.Apis.Gmail.v1.Data.Message message)
    {
        var summary = new EmailSummary
        {
            Id = message.Id,
            ThreadId = message.ThreadId,
            Snippet = message.Snippet ?? string.Empty,
            Labels = message.LabelIds?.ToList() ?? new List<string>(),
            IsUnread = message.LabelIds?.Contains("UNREAD") ?? false,
            HasAttachments = message.Payload?.Parts?.Any(p => !string.IsNullOrEmpty(p.Filename)) ?? false
        };

        // Parse headers
        if (message.Payload?.Headers != null)
        {
            foreach (var header in message.Payload.Headers)
            {
                switch (header.Name?.ToLowerInvariant())
                {
                    case "from":
                        summary.From = header.Value ?? string.Empty;
                        break;
                    case "subject":
                        summary.Subject = header.Value ?? string.Empty;
                        break;
                    case "date":
                        if (DateTime.TryParse(header.Value, out var date))
                        {
                            summary.ReceivedAt = date;
                        }
                        break;
                }
            }
        }

        // Fallback: use internal date if Date header wasn't parsed
        if (summary.ReceivedAt == default && message.InternalDate.HasValue)
        {
            summary.ReceivedAt = DateTimeOffset.FromUnixTimeMilliseconds(message.InternalDate.Value).DateTime;
        }

        return summary;
    }

    private Google.Apis.Gmail.v1.GmailService? GetGmailApi()
    {
        if (_gmailApi != null)
            return _gmailApi;

        var credential = _authService.GetCredential();
        if (credential == null)
            return null;

        _gmailApi = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "BrickPrinterApp"
        });

        return _gmailApi;
    }
}
