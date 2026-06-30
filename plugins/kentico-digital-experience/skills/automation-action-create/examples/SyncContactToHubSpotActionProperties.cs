using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="SyncContactToHubSpotAction"/>.
/// </summary>
public class SyncContactToHubSpotActionProperties : IAutomationActionProperties
{
    [TextInputComponent(
        Label = "Target list ID",
        ExplanationText = "HubSpot static list identifier the contact should be added to after upsert.",
        Order = 1)]
    [RequiredValidationRule]
    public string TargetListId { get; set; } = "";


    [CheckBoxComponent(Label = "Include email", Order = 2)]
    public bool IncludeEmail { get; set; } = true;


    [CheckBoxComponent(Label = "Include phone", Order = 3)]
    public bool IncludePhone { get; set; } = false;


    [CheckBoxComponent(Label = "Include company", Order = 4)]
    public bool IncludeCompany { get; set; } = false;
}
