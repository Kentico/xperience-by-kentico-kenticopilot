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
