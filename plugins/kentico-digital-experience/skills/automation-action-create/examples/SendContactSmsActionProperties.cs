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
