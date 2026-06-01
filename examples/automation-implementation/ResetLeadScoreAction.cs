using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<ResetLeadScoreAction>(
    identifier: "DancingGoat.ResetLeadScore",
    displayName: "Reset lead score",
    IconName = "xp-arrow-u-left",
    Tooltip = "Resets the contact's accumulated lead score back to zero. Pairs with the Update lead score action.")]

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Automation action that resets <see cref="LeadScoringData"/> back to a zero state.
/// Pairs with <see cref="UpdateLeadScoreAction"/> and uses the no-properties <see cref="AutomationAction"/> base class.
/// </summary>
public class ResetLeadScoreAction(ILogger<ResetLeadScoreAction> logger) : AutomationAction
{
    public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        if (context.ProcessedObject is not ContactInfo contact)
        {
            logger.LogWarning(
                "Skipping ResetLeadScore — processed object is not a contact (got '{ObjectType}').",
                context.ProcessedObject?.TypeInfo.ObjectType);
            return;
        }

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
