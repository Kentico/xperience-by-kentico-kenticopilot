using CMS.Automation;
using CMS.DataProtection;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Kentico.Xperience.DancingGoat.Automation;


/// <summary>
/// Configurable properties for <see cref="UpdateContactConsentAction"/>.
/// </summary>
public class UpdateContactConsentActionProperties : IAutomationActionProperties
{
    [ObjectIdSelectorComponent(
        ConsentInfo.OBJECT_TYPE,
        Label = "Consent",
        Order = 1)]
    [RequiredValidationRule]
    public int? Consent { get; set; }


    [DropDownComponent(
        Label = "Action",
        Options = "Agree;Grant consent\nRevoke;Revoke consent",
        Order = 2)]
    public string Action { get; set; } = UpdateContactConsentAction.AgreeAction;
}
