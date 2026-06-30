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
