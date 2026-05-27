using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.DancingGoat.Automation;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<UpdateLeadScoreAction>(
    identifier: "DancingGoat.UpdateLeadScore",
    displayName: "Update lead score",
    IconName = "xp-arrow-up",
    Tooltip = "Adds (or subtracts) points from the contact's accumulated lead score persisted across automation steps.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="UpdateLeadScoreAction"/>.
/// </summary>
public class UpdateLeadScoreActionProperties : IAutomationActionProperties
{
    [NumberInputComponent(Label = "Points", ExplanationText = "Use a negative value to subtract.", Order = 1)]
    public int Points { get; set; } = 10;
}


/// <summary>
/// Automation action that adds points to the contact's accumulated lead score,
/// persisted across automation steps as <see cref="LeadScoringData"/>.
/// </summary>
public class UpdateLeadScoreAction(ILogger<UpdateLeadScoreAction> logger) : AutomationAction<UpdateLeadScoreActionProperties>
{
    public override async Task Execute(
        UpdateLeadScoreActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (context.ProcessedObject is not ContactInfo contact)
        {
            logger.LogWarning(
                "Skipping UpdateLeadScore — processed object is not a contact (got '{ObjectType}').",
                context.ProcessedObject?.TypeInfo.ObjectType);
            return;
        }

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
