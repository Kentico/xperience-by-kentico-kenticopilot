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
