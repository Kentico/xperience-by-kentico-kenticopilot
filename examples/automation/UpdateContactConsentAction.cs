using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DataProtection;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.DancingGoat.Automation;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<UpdateContactConsentAction>(
    identifier: "DancingGoat.UpdateContactConsent",
    displayName: "Update contact consent",
    IconName = "xp-check-circle",
    Tooltip = "Grants or revokes the selected consent for the contact via IConsentAgreementService.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Action to take on the selected consent.
/// </summary>
public enum ConsentActionOption
{
    [Description("Grant consent")]
    Agree,

    [Description("Revoke consent")]
    Revoke,
}


/// <summary>
/// Configurable properties for <see cref="UpdateContactConsentAction"/>.
/// </summary>
public class UpdateContactConsentActionProperties : IAutomationActionProperties
{
    [ObjectIdSelectorComponent(
        ConsentInfo.OBJECT_TYPE,
        Label = "Consent",
        Order = 1)]
    public IEnumerable<int> Consent { get; set; } = [];


    [DropDownComponent(
        Label = "Action",
        Options = "Agree;Grant consent\nRevoke;Revoke consent",
        Order = 2)]
    public string Action { get; set; } = nameof(ConsentActionOption.Agree);
}


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
    public override Task Execute(
        UpdateContactConsentActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (context.ProcessedObject is not ContactInfo contact)
        {
            logger.LogWarning(
                "Skipping UpdateContactConsent — processed object is not a contact (got '{ObjectType}').",
                context.ProcessedObject?.TypeInfo.ObjectType);
            return Task.CompletedTask;
        }

        var consentId = properties.Consent?.FirstOrDefault() ?? 0;
        if (consentId <= 0)
        {
            logger.LogWarning("Skipping UpdateContactConsent for contact {ContactId} — no consent selected on the step.", contact.ContactID);
            return Task.CompletedTask;
        }

        var consent = consentInfoProvider.Get(consentId);
        if (consent is null)
        {
            logger.LogWarning("Skipping UpdateContactConsent for contact {ContactId} — consent {ConsentId} not found.", contact.ContactID, consentId);
            return Task.CompletedTask;
        }

        if (string.Equals(properties.Action, nameof(ConsentActionOption.Revoke), System.StringComparison.OrdinalIgnoreCase))
        {
            consentAgreementService.Revoke(contact, consent);
            logger.LogInformation("Revoked consent '{ConsentName}' for contact {ContactId}.", consent.ConsentName, contact.ContactID);
        }
        else
        {
            consentAgreementService.Agree(contact, consent);
            logger.LogInformation("Granted consent '{ConsentName}' for contact {ContactId}.", consent.ConsentName, contact.ContactID);
        }

        return Task.CompletedTask;
    }
}
