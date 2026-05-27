using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.DancingGoat.Automation;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<NotifySalesOnSlackAction>(
    identifier: "DancingGoat.NotifySalesOnSlack",
    displayName: "Notify sales on Slack",
    IconName = "xp-bell",
    Tooltip = "Posts a templated message to a configured Slack incoming webhook when a contact reaches this step.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="NotifySalesOnSlackAction"/>.
/// </summary>
public class NotifySalesOnSlackActionProperties : IAutomationActionProperties
{
    [TextInputComponent(
        Label = "Webhook URL",
        ExplanationText = "Slack incoming webhook URL (https://hooks.slack.com/services/...).",
        Order = 1)]
    [RequiredValidationRule]
    public string WebhookUrl { get; set; } = "";


    [TextAreaComponent(
        Label = "Message template",
        ExplanationText = "Use {ContactDescriptiveName} as a placeholder for the contact's display name.",
        Order = 2)]
    [RequiredValidationRule]
    public string MessageTemplate { get; set; } = "Hot lead reached the qualification step: {ContactDescriptiveName}";
}


/// <summary>
/// Automation action that POSTs a templated message to a Slack
/// <see href="https://api.slack.com/messaging/webhooks">incoming webhook</see>.
/// Uses a typed <see cref="HttpClient"/> registered via
/// <c>services.AddHttpClient&lt;NotifySalesOnSlackAction&gt;()</c>.
/// </summary>
public class NotifySalesOnSlackAction(
    HttpClient httpClient,
    ILogger<NotifySalesOnSlackAction> logger)
    : AutomationAction<NotifySalesOnSlackActionProperties>
{
    public override async Task Execute(
        NotifySalesOnSlackActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (context.ProcessedObject is not ContactInfo contact)
        {
            logger.LogWarning(
                "Skipping NotifySalesOnSlack — processed object is not a contact (got '{ObjectType}').",
                context.ProcessedObject?.TypeInfo.ObjectType);
            return;
        }

        var text = properties.MessageTemplate
            .Replace("{ContactDescriptiveName}", contact.ContactDescriptiveName ?? $"Contact #{contact.ContactID}");

        // Slack incoming webhooks accept a simple JSON payload with a "text" field. For richer
        // formatting (sections, fields, attachments), use Block Kit:
        // https://api.slack.com/messaging/webhooks#advanced_message_formatting
        var payload = new { text };

        var response = await httpClient.PostAsJsonAsync(properties.WebhookUrl, payload, cancellationToken);

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
