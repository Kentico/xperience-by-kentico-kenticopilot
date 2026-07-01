# Example automation actions

Canonical custom automation action samples covering distinct patterns. The first five extend `AutomationAction<TProperties>`; `Reset lead score` uses the no-properties `AutomationAction` base class. Each action and its properties class live in separate files, per the one-class-per-file guardrail.

## Send SMS to contact

SendContactSmsAction.cs

```csharp
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

[assembly: RegisterAutomationAction<SendContactSmsAction>(
    identifier: SendContactSmsAction.IDENTIFIER,
    displayName: "Send SMS to contact",
    IconName = Icons.Message,
    Description = "Sends a templated SMS to the contact's mobile phone via Twilio.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that sends a templated SMS to the contact via the Twilio SDK.
/// Reads the contact's phone number from <see cref="ContactInfo.ContactMobilePhone"/>
/// (falling back to <see cref="ContactInfo.ContactBusinessPhone"/>), substitutes placeholders
/// in the message template, and dispatches the message through
/// <c>Twilio.Rest.Api.V2010.Account.MessageResource.CreateAsync</c>.
/// </summary>
/// <remarks>
/// The host registers <see cref="ITwilioRestClient"/> once and the DI container manages its
/// lifetime. A typical wiring in <c>Program.cs</c> reads credentials from a typed
/// <c>TwilioOptions</c> bound to <c>appsettings.json</c>:
/// <code>
/// services.Configure&lt;TwilioOptions&gt;(builder.Configuration.GetSection("Twilio"));
/// services.AddSingleton&lt;ITwilioRestClient&gt;(sp =&gt;
/// {
///     var opts = sp.GetRequiredService&lt;IOptions&lt;TwilioOptions&gt;&gt;().Value;
///     return new TwilioRestClient(opts.AccountSid, opts.AuthToken);
/// });
/// </code>
/// This keeps a single container-managed client instance and respects the concurrency guardrail
/// (no per-execution mutable state in the action; no global static init from inside Execute).
/// </remarks>
public class SendContactSmsAction(
    ITwilioRestClient twilioRestClient,
    ILogger<SendContactSmsAction> logger)
    : AutomationAction<SendContactSmsActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.SendContactSms";


    public override async Task Execute(
        SendContactSmsActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        var phone = ResolveContactPhone(contact);
        if (string.IsNullOrWhiteSpace(phone))
        {
            logger.LogWarning("Skipping SendContactSms for contact {ContactId} — no phone number on the contact.", contact.ContactID);
            return;
        }

        var body = properties.MessageTemplate.Replace(
            "{ContactFirstName}",
            string.IsNullOrEmpty(contact.ContactFirstName) ? "valued customer" : contact.ContactFirstName);

        var message = await MessageResource.CreateAsync(
            to: new PhoneNumber(phone),
            from: new PhoneNumber(properties.SenderId),
            body: body,
            client: twilioRestClient);

        logger.LogInformation(
            "Twilio SMS sent for contact {ContactId} to {RecipientPhone} (sid {MessageSid}, status {Status}).",
            contact.ContactID,
            phone,
            message.Sid,
            message.Status);
    }


    private static string ResolveContactPhone(ContactInfo contact) =>
        !string.IsNullOrWhiteSpace(contact.ContactMobilePhone)
            ? contact.ContactMobilePhone
            : contact.ContactBusinessPhone;
}
```

SendContactSmsActionProperties.cs

```csharp
using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="SendContactSmsAction"/>.
/// </summary>
public class SendContactSmsActionProperties : IAutomationActionProperties
{
    [TextInputComponent(
        Label = "Sender ID",
        ExplanationText = "Twilio \"from\" number in E.164 format (or an alphanumeric sender ID where supported).",
        Order = 1)]
    [RequiredValidationRule]
    public string SenderId { get; set; } = "";


    [TextAreaComponent(
        Label = "Message template",
        ExplanationText = "Use {ContactFirstName} as a placeholder for the contact's first name.",
        Order = 2)]
    [RequiredValidationRule]
    public string MessageTemplate { get; set; } = "Hi {ContactFirstName}, thanks for being a Dancing Goat customer!";
}
```

## Notify sales on Slack

NotifySalesOnSlackAction.cs

```csharp
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
```

NotifySalesOnSlackActionProperties.cs

```csharp
using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="NotifySalesOnSlackAction"/>.
/// </summary>
/// <remarks>
/// The Slack webhook URL embeds a tokenized path and is treated as a secret — it lives in
/// <c>appsettings.json</c> (bound to <c>SlackOptions</c>), not on the step. Marketers only edit
/// the message template here.
/// </remarks>
public class NotifySalesOnSlackActionProperties : IAutomationActionProperties
{
    [TextAreaComponent(
        Label = "Message template",
        ExplanationText = "Use {ContactDescriptiveName} as a placeholder for the contact's display name.",
        Order = 1)]
    [RequiredValidationRule]
    public string MessageTemplate { get; set; } = "Hot lead reached the qualification step: {ContactDescriptiveName}";
}
```

## Sync contact to HubSpot

SyncContactToHubSpotAction.cs

```csharp
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<SyncContactToHubSpotAction>(
    identifier: SyncContactToHubSpotAction.IDENTIFIER,
    displayName: "Sync contact to HubSpot",
    IconName = Icons.ArrowRightTopSquare,
    Description = "Pushes the contact's mapped fields to a HubSpot list. Updates the contact when it already exists (keyed by email).")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Outcome of the HubSpot sync.
/// </summary>
public sealed class CrmSyncResult
{
    public string RecordId { get; init; } = "";

    public bool Success { get; init; }
}


/// <summary>
/// Which contact fields to include when pushing to HubSpot.
/// </summary>
public sealed class ContactFieldMapping
{
    public bool IncludeEmail { get; init; }

    public bool IncludePhone { get; init; }

    public bool IncludeCompany { get; init; }
}


/// <summary>
/// Automation action that pushes the contact to a HubSpot list. Orchestrates HubSpot CRM v3
/// primitives exposed by <see cref="HubSpotCrmSyncService"/>: builds the property payload,
/// creates the contact, falls through to a PATCH (keyed by email) when the contact already exists,
/// and optionally adds the resulting HubSpot record to the configured list.
/// </summary>
public class SyncContactToHubSpotAction(
    HubSpotCrmSyncService crmService,
    ILogger<SyncContactToHubSpotAction> logger)
    : AutomationAction<SyncContactToHubSpotActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.SyncContactToHubSpot";


    public override async Task Execute(
        SyncContactToHubSpotActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        var mapping = new ContactFieldMapping
        {
            IncludeEmail = properties.IncludeEmail,
            IncludePhone = properties.IncludePhone,
            IncludeCompany = properties.IncludeCompany,
        };

        var result = await UpsertContactAsync(contact, properties.TargetListId, mapping, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation(
                "Synced contact {ContactId} to HubSpot list '{TargetListId}' (record {RecordId}).",
                contact.ContactID,
                properties.TargetListId,
                result.RecordId);
        }
        else
        {
            logger.LogError(
                "HubSpot sync failed for contact {ContactId} to list '{TargetListId}'.",
                contact.ContactID,
                properties.TargetListId);
        }
    }


    private async Task<CrmSyncResult> UpsertContactAsync(
        ContactInfo contact,
        string targetListId,
        ContactFieldMapping mapping,
        CancellationToken cancellationToken)
    {
        var properties = HubSpotCrmSyncService.BuildProperties(contact, mapping);

        // Try to create. HubSpot returns 409 when a contact with the same email already exists —
        // the service signals that by returning null so we can switch to a PATCH (idempotent upsert).
        var hubSpotContact = await crmService.CreateContactAsync(properties, cancellationToken);
        if (hubSpotContact is null)
        {
            hubSpotContact = await crmService.UpdateContactByEmailAsync(contact.ContactEmail, properties, cancellationToken);
        }

        if (hubSpotContact is null)
        {
            return new CrmSyncResult { Success = false };
        }

        if (!string.IsNullOrWhiteSpace(targetListId))
        {
            await crmService.AddToListAsync(hubSpotContact.Id, targetListId, cancellationToken);
        }

        return new CrmSyncResult
        {
            RecordId = hubSpotContact.Id,
            Success = true
        };
    }
}
```

SyncContactToHubSpotActionProperties.cs

```csharp
using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="SyncContactToHubSpotAction"/>.
/// </summary>
public class SyncContactToHubSpotActionProperties : IAutomationActionProperties
{
    [TextInputComponent(
        Label = "Target list ID",
        ExplanationText = "HubSpot static list identifier the contact should be added to after upsert.",
        Order = 1)]
    [RequiredValidationRule]
    public string TargetListId { get; set; } = "";


    [CheckBoxComponent(Label = "Include email", Order = 2)]
    public bool IncludeEmail { get; set; } = true;


    [CheckBoxComponent(Label = "Include phone", Order = 3)]
    public bool IncludePhone { get; set; } = false;


    [CheckBoxComponent(Label = "Include company", Order = 4)]
    public bool IncludeCompany { get; set; } = false;
}
```

## Update contact consent

UpdateContactConsentAction.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<UpdateContactConsentAction>(
    identifier: UpdateContactConsentAction.IDENTIFIER,
    displayName: "Update contact consent",
    IconName = Icons.CheckCircle,
    Description = "Grants or revokes the selected consent for the contact via IConsentAgreementService.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that grants or revokes a GDPR consent for the contact
/// via <see cref="IConsentAgreementService"/>.
/// </summary>
public class UpdateContactConsentAction(
    IConsentAgreementService consentAgreementService,
    IInfoProvider<ConsentInfo> consentInfoProvider,
    ILogger<UpdateContactConsentAction> logger)
    : AutomationAction<UpdateContactConsentActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.UpdateContactConsent";

    internal const string AgreeAction = "Agree";
    internal const string RevokeAction = "Revoke";


    public override async Task Execute(
        UpdateContactConsentActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        if (properties.Consent is not int consentId)
        {
            logger.LogWarning("Skipping UpdateContactConsent for contact {ContactId} — no consent selected on the step.", contact.ContactID);
            return;
        }

        var consent = consentInfoProvider.Get(consentId);
        if (consent is null)
        {
            logger.LogWarning("Skipping UpdateContactConsent for contact {ContactId} — consent {ConsentId} not found.", contact.ContactID, consentId);
            return;
        }

        if (string.Equals(properties.Action, RevokeAction, StringComparison.OrdinalIgnoreCase))
        {
            consentAgreementService.Revoke(contact, consent);
            logger.LogInformation("Revoked consent '{ConsentName}' for contact {ContactId}.", consent.ConsentName, contact.ContactID);
        }
        else
        {
            consentAgreementService.Agree(contact, consent);
            logger.LogInformation("Granted consent '{ConsentName}' for contact {ContactId}.", consent.ConsentName, contact.ContactID);
        }
    }
}
```

UpdateContactConsentActionProperties.cs

```csharp
using CMS.Automation;
using CMS.DataProtection;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="UpdateContactConsentAction"/>.
/// </summary>
public class UpdateContactConsentActionProperties : IAutomationActionProperties
{
    [ObjectIdSelectorComponent(
        ConsentInfo.OBJECT_TYPE,
        Label = "Consent",
        Order = 1)]
    [RequiredValidationRule]
    public int? Consent { get; set; }


    [DropDownComponent(
        Label = "Action",
        Options = "Agree;Grant consent\nRevoke;Revoke consent",
        Order = 2)]
    public string Action { get; set; } = UpdateContactConsentAction.AgreeAction;
}
```

## Update lead score

UpdateLeadScoreAction.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<UpdateLeadScoreAction>(
    identifier: UpdateLeadScoreAction.IDENTIFIER,
    displayName: "Update lead score",
    IconName = Icons.ArrowUp,
    Description = "Adds (or subtracts) points from the contact's accumulated lead score persisted across automation steps.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that adds points to the contact's accumulated lead score,
/// persisted across automation steps as <see cref="LeadScoringData"/>.
/// </summary>
public class UpdateLeadScoreAction(ILogger<UpdateLeadScoreAction> logger) : AutomationAction<UpdateLeadScoreActionProperties>
{
    public const string IDENTIFIER = "DancingGoat.UpdateLeadScore";


    public override async Task Execute(
        UpdateLeadScoreActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        var data = await context.GetProcessData<LeadScoringData>(cancellationToken) ?? new LeadScoringData();

        data.Score += properties.Points;
        data.LastUpdatedAt = DateTime.UtcNow;

        await context.SetProcessData(data, cancellationToken);

        logger.LogInformation(
            "Lead score updated for contact {ContactId}: added {Points} points, new total {Score}.",
            contact.ContactID,
            properties.Points,
            data.Score);
    }
}
```

UpdateLeadScoreActionProperties.cs

```csharp
using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="UpdateLeadScoreAction"/>.
/// </summary>
public class UpdateLeadScoreActionProperties : IAutomationActionProperties
{
    [NumberInputComponent(Label = "Points", ExplanationText = "Use a negative value to subtract.", Order = 1)]
    public int Points { get; set; } = 10;
}
```

## Reset lead score

ResetLeadScoreAction.cs

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<ResetLeadScoreAction>(
    identifier: ResetLeadScoreAction.IDENTIFIER,
    displayName: "Reset lead score",
    IconName = Icons.ArrowULeft,
    Description = "Resets the contact's accumulated lead score back to zero. Pairs with the Update lead score action.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that resets <see cref="LeadScoringData"/> back to a zero state.
/// Pairs with <see cref="UpdateLeadScoreAction"/> and uses the no-properties <see cref="AutomationAction"/> base class.
/// </summary>
public class ResetLeadScoreAction(ILogger<ResetLeadScoreAction> logger) : AutomationAction
{
    public const string IDENTIFIER = "DancingGoat.ResetLeadScore";


    public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        ContactInfo contact = await context.GetProcessedObject(cancellationToken);

        await context.SetProcessData(
            new LeadScoringData
            {
                Score = 0,
                LastUpdatedAt = DateTime.UtcNow
            },
            cancellationToken);

        logger.LogInformation("Lead score reset to zero for contact {ContactId}.", contact.ContactID);
    }
}
```

