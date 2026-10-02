namespace BrickPrinterApp.Interfaces;

/// <summary>
/// Service for retrieving emails from Gmail
/// </summary>
public interface IGmailService
{
    /// <summary>
    /// Gets the most recent emails from the user's inbox
    /// </summary>
    /// <param name="maxResults">Maximum number of emails to retrieve (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of email summaries</returns>
    Task<List<EmailSummary>> GetEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread emails from the user's inbox
    /// </summary>
    /// <param name="maxResults">Maximum number of emails to retrieve (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread email summaries</returns>
    Task<List<EmailSummary>> GetUnreadEmailsAsync(int maxResults = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of unread emails
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of unread emails</returns>
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Gmail service is available (user is authenticated with Gmail scope)
    /// </summary>
    bool IsAvailable { get; }
}

/// <summary>
/// Summary of an email message
/// </summary>
public class EmailSummary
{
    /// <summary>
    /// Unique message ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Thread ID this message belongs to
    /// </summary>
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Sender's email address and name
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Snippet/preview of the email body
    /// </summary>
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// When the email was received
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Whether the email has been read
    /// </summary>
    public bool IsUnread { get; set; }

    /// <summary>
    /// Whether the email has attachments
    /// </summary>
    public bool HasAttachments { get; set; }

    /// <summary>
    /// List of label IDs on this message
    /// </summary>
    public List<string> Labels { get; set; } = new();
}
