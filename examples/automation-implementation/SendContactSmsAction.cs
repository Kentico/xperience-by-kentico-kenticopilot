using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base.FormAnnotations;

using Microsoft.Extensions.Logging;

using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

[assembly: RegisterAutomationAction<SendContactSmsAction>(
    identifier: "DancingGoat.SendContactSms",
    displayName: "Send SMS to contact",
    IconName = "xp-message",
    Tooltip = "Sends a templated SMS to the contact's mobile phone via Twilio.")]

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
    public override async Task Execute(
        SendContactSmsActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (context.ProcessedObject is not ContactInfo contact)
        {
            logger.LogWarning(
                "Skipping SendContactSms — processed object is not a contact (got '{ObjectType}').",
                context.ProcessedObject?.TypeInfo.ObjectType);
            return;
        }

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
