using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="UpdateLeadScoreAction"/>.
/// </summary>
public class UpdateLeadScoreActionProperties : IAutomationActionProperties
{
    [NumberInputComponent(Label = "Points", ExplanationText = "Use a negative value to subtract.", Order = 1)]
    public int Points { get; set; } = 10;
}
