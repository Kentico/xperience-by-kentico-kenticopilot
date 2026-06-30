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
