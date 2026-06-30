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
