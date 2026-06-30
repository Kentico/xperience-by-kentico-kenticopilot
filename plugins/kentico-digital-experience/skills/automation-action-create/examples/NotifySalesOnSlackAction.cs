using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: RegisterAutomationAction<NotifySalesOnSlackAction>(
    identifier: NotifySalesOnSlackAction.IDENTIFIER,
    displayName: "Notify sales on Slack",
    IconName = Icons.Bell,
    Description = "Posts a templated message to the configured Slack incoming webhook when a contact reaches this step.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that POSTs a templated message to a Slack
/// <see href="https://api.slack.com/messaging/webhooks">incoming webhook</see>.
/// Uses a typed <see cref="HttpClient"/> registered via
/// <c>services.AddHttpClient&lt;NotifySalesOnSlackAction&gt;()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The host wires the webhook URL through a typed <c>SlackOptions</c> (with a <c>WebhookUrl</c>
/// property) bound to the <c>Slack</c> section of <c>appsettings.json</c>:
/// </para>
/// <code>
/// services.Configure&lt;SlackOptions&gt;(builder.Configuration.GetSection("Slack"));
/// services.AddHttpClient&lt;NotifySalesOnSlackAction&gt;();
/// </code>
/// </remarks>
public class NotifySalesOnSlackAction(
    HttpClient httpClient,
    IOptions<SlackOptions> slackOptions,
    ILogger<NotifySalesOnSlackAction> logger)
    : AutomationAction<NotifySalesOnSlackActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.NotifySalesOnSlack";

    private readonly SlackOptions slackOptions = slackOptions.Value;


    public override async Task Execute(
        NotifySalesOnSlackActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        var text = properties.MessageTemplate
            .Replace("{ContactDescriptiveName}", contact.ContactDescriptiveName ?? $"Contact #{contact.ContactID}");

        // Slack incoming webhooks accept a simple JSON payload with a "text" field. For richer
        // formatting (sections, fields, attachments), use Block Kit:
        // https://api.slack.com/messaging/webhooks#advanced_message_formatting
        var payload = new { text };

        var response = await httpClient.PostAsJsonAsync(slackOptions.WebhookUrl, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "NotifySalesOnSlack failed for contact {ContactId}: webhook returned HTTP {StatusCode}.",
                contact.ContactID,
                (int)response.StatusCode);
            return;
        }

        logger.LogInformation("Posted Slack notification for contact {ContactId}.", contact.ContactID);
    }
}
