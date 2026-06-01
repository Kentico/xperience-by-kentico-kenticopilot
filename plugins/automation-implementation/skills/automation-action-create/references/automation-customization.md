---
    Snapshot of the documentation. Replace this file by the documentation reference in docs/
---

Xperience by Kentico allows developers to extend the marketing {% page_link automation_xp linkText="automation" %} engine with **custom actions**. These actions appear in the Automation Builder UI alongside built-in steps (Send email, Wait, Condition, etc.) and can be configured by marketers.

Common use cases for custom automation actions include:

- Synchronizing contact data to external CRMs
- Sending notification emails or SMS to internal users
- Enriching contact profiles from third-party data sources
- Logging analytics events to external platforms
- Cleaning up process data at the end of a workflow

## Custom automation component overview

**

{% image automation_components_overview.drawio.svg title="Overview of custom automation action components" width="600" border=true %}

{% tip %}
**Key points**

- Inherit from `AutomationAction` (no properties) or `AutomationAction<TProperties>` (with configurable properties).
- Register actions via the `RegisterAutomationAction` assembly attribute.
- Use `AutomationProcessContext` to access the processed contact and share data between steps.
- Actions support full dependency injection through constructors.
{% endtip %}

## Custom automation actions

Custom actions are classes that implement specific logic executed when a contact reaches the corresponding step in an automation process. Two patterns are available:

- **Actions without configurable properties** -- inherit from `AutomationAction`. Use when the action's behavior is fully defined in code.
- **Actions with configurable properties** -- inherit from `AutomationAction<TProperties>`. Use when marketers need to configure the action's behavior through the Automation Builder UI.

Both patterns support dependency injection -- the automation engine resolves action instances from the DI container, allowing constructor injection of any registered service.

### Create a custom action without properties

To create an action that requires no marketer-configurable settings:

1. Create a class inheriting from `AutomationAction` (in the `CMS.Automation` namespace).
2. Override the `Execute` method.
3. {% inpage_link "Register custom actions" linkText="Register the action" %} via the `RegisterAutomationAction` assembly attribute.

{% code lang=csharp title="AuditLogAction.cs" %}
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<AuditLogAction>(
    identifier: "Acme_Audit_LogContactReachedStep",
    displayName: "Log to audit system",
    Tooltip = "Sends a lightweight audit event to the external logging service.")]

namespace Acme.Automation;

public class AuditLogAction : AutomationAction
{
    private readonly ILogger<AuditLogAction> logger;

    public AuditLogAction(ILogger<AuditLogAction> logger)
    {
        this.logger = logger;
    }

    public override Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        ContactInfo contact = context.ProcessedObject as ContactInfo;
        if (contact is null)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Contact {ContactId} reached audit step.", contact.ContactID);

        return Task.CompletedTask;
    }
}
{% endcode %}

### Create a custom action with properties

To create an action with settings configurable in the Automation Builder UI:

1. Create a properties class implementing `IAutomationActionProperties`.
2. Decorate its properties with {% page_link 6ASiCQ linkText="form component" %} attributes.
3. Create an action class inheriting from `AutomationAction<TProperties>`.
4. Override the `Execute` method, which receives the properties instance populated with values from the step configuration.
5. {% inpage_link "Register custom actions" linkText="Register the action" %} via the `RegisterAutomationAction` assembly attribute.

{% code lang=csharp title="NotifyMarketerAction.cs" %}
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterAutomationAction<NotifyMarketerAction>(
    identifier: "Acme_Notifications_NotifyMarketer",
    displayName: "Notify marketer via email",
    IconName = "xp-mail")]

namespace Acme.Automation;

public class NotifyMarketerActionProperties : IAutomationActionProperties
{
    [TextInputComponent(Label = "Recipient email", Order = 1)]
    [RequiredValidationRule]
    public string RecipientEmail { get; set; }

    [TextInputComponent(Label = "Subject", Order = 2)]
    public string Subject { get; set; } = "Automation notification";

    [TextAreaComponent(Label = "Message body", Order = 3)]
    public string Body { get; set; }
}

public class NotifyMarketerAction : AutomationAction<NotifyMarketerActionProperties>
{
    private readonly IEmailService emailService;

    public NotifyMarketerAction(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    public override async Task Execute(
        NotifyMarketerActionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(properties.RecipientEmail))
        {
            return;
        }

        ContactInfo contact = context.ProcessedObject as ContactInfo;
        string body = $"{properties.Body}\n\nContact: {contact?.ContactDescriptiveName}";

        await emailService.SendAsync(properties.RecipientEmail, properties.Subject, body, cancellationToken);
    }
}
{% endcode %}

## Register custom actions

Register custom actions using the `RegisterAutomationActionAttribute<TAction>` assembly attribute. You can place the attribute at the top of the action's source file or in a central registration class (e.g., `ComponentRegister.cs`).

Specify the following parameters:

- `identifier` -- a unique, stable string identifier for the action. Use the format `CompanyName_ModuleName_ActionName` (e.g., `Acme_Crm_SyncContactAction`). Must be a valid code name (letters, digits, underscores, dots). **Never change this value once the action is deployed and used in automation processes.**
- `displayName` -- the name displayed in the Automation Builder step palette.

You can also set the following optional properties:

- `IconName` -- the icon displayed for the action tile in the builder. Use Xperience icon names with the *xp-* prefix (defaults to *xp-cogwheel*).
- `Tooltip` -- hover text displayed in the step palette.

{% code lang=csharp title="Registration example" %}
using CMS.Automation;

[assembly: RegisterAutomationAction<SyncContactToCrmAction>(
    identifier: "Acme_Crm_SyncContact",
    displayName: "Sync contact to CRM",
    IconName = "xp-arrow-right-top-square",
    Tooltip = "Pushes the current contact's data to the external CRM.")]
{% endcode %}

Once registered, the action appears in the step selection dialog when adding steps in the Automation Builder.

{% warning %}
Deleting or unregistering an action that is used in an existing automation process breaks the process. Contacts currently in the step or waiting to reach it will encounter errors.
{% endwarning %}

## Action properties

Action properties define what marketers can configure for each instance of a custom action step. Create a class implementing the `IAutomationActionProperties` marker interface and decorate its properties with {% page_link 6ASiCQ linkText="editing component" %} attributes.

### Supported form components

The following form components are available for action properties:

- `TextInputComponent` -- single-line text input
- `TextAreaComponent` -- multi-line text input
- `NumberInputComponent` -- integer input
- `DecimalNumberInputComponent` -- decimal number input
- `DropDownComponent` -- drop-down selection (static options or dynamic via `IDropDownOptionsProvider`)
- `RadioGroupComponent` -- radio button selection
- `CheckBoxComponent` -- boolean toggle
- `DateInputComponent` -- date picker
- `DateTimeInputComponent` -- date and time picker
- `RichTextEditorComponent` -- rich text editor

### Validation and UI organization

You can apply validation rules and organize properties in the configuration dialog:

- **Validation rules** -- `RequiredValidationRule`, `MaxLengthValidationRule`, `MinimumIntegerValueValidationRule`, `MaximumIntegerValueValidationRule`, `MinimumDecimalValueValidationRule`, etc.
- **Form categories** -- `[FormCategory]` attributes group properties under labeled sections. Categories support `Collapsible` and `IsCollapsed` options.
- **Conditional visibility** -- `[VisibleIfTrue]` shows a property only when a specified boolean property is checked.

{% tip %}
**Advanced property options**

You can add dynamic {% page_link 54LWCQ linkText="visibility conditions and validation" %} that restricts how and when properties are displayed in the action configuration dialog.
{% endtip %}

### Customize the step name field

By default, the step configuration dialog includes a **Step name** text input. If your properties class defines a property named `StepDisplayName`, its form component annotation (label, watermark, validation) replaces the default step name input.

### Property mapping rules

Only public properties with both a getter and a setter participate in mapping. Properties are serialized as JSON. Default values set on properties are used for new step instances.

{% code lang=csharp title="Comprehensive properties example" %}
using CMS.Automation;

using Kentico.Xperience.Admin.Base.FormAnnotations;

[FormCategory(Label = "General", Order = 10)]
[FormCategory(Label = "Advanced", Order = 50, Collapsible = true, IsCollapsed = true)]
public class CrmSyncActionProperties : IAutomationActionProperties
{
    // Overrides the default step name input with a custom label and watermark
    [TextInputComponent(Label = "Step name", WatermarkText = "Enter step name...", Order = 1)]
    [RequiredValidationRule]
    [MaxLengthValidationRule(450)]
    public string StepDisplayName { get; set; }

    [DropDownComponent(Label = "CRM System",
        Options = "salesforce;Salesforce\nhubspot;HubSpot\ndynamics;Dynamics 365",
        Order = 11)]
    [RequiredValidationRule]
    public string CrmSystem { get; set; } = "salesforce";

    [CheckBoxComponent(Label = "Update existing records", Order = 12)]
    public bool UpdateExisting { get; set; } = true;

    [NumberInputComponent(Label = "Retry attempts", WatermarkText = "3", Order = 51)]
    [MinimumIntegerValueValidationRule(0)]
    [MaximumIntegerValueValidationRule(10)]
    public int RetryAttempts { get; set; } = 3;

    [RichTextEditorComponent(Label = "Notes", Order = 52)]
    [VisibleIfTrue(nameof(UpdateExisting))]
    public string Notes { get; set; }
}
{% endcode %}

## Runtime context

The `AutomationProcessContext` class is provided to every `Execute` call and gives access to runtime data:

{% table %}
    {% row header=true %}
        {% cell %}
        Member
        {% endcell %}

        {% cell %}
        Type
        {% endcell %}

        {% cell %}
        Description
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `ProcessedObject`
        {% endcell %}

        {% cell %}
        `BaseInfo`
        {% endcell %}

        {% cell %}
        The object being processed by the automation (typically a `ContactInfo`).
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `GetProcessData<T>(CancellationToken)`
        {% endcell %}

        {% cell %}
        `Task<T>`
        {% endcell %}

        {% cell %}
        Retrieves persisted cross-step data. Returns `null` if not found.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `SetProcessData<T>(T, CancellationToken)`
        {% endcell %}

        {% cell %}
        `Task`
        {% endcell %}

        {% cell %}
        Stores typed data that persists across steps and wait periods.
        {% endcell %}
    {% endrow %}
{% endtable %}

### Access contact data

The processed object is typically a `ContactInfo` instance. Always check for `null` to handle edge cases gracefully:

{% code lang=csharp %}
ContactInfo contact = context.ProcessedObject as ContactInfo;
if (contact is null)
{
    // Not a contact — handle gracefully or return early
    return;
}

// Access contact properties
string contactName = contact.ContactDescriptiveName;
int contactId = contact.ContactID;
{% endcode %}

## Share data between automation steps

Custom actions within the same automation process can share typed data using the `AutomationProcessContext`. This allows one action to produce data that a later action consumes, even if wait steps or other actions are between them.

### How process data works

- Data is persisted in the database as JSON for the duration of the contact's process execution.
- Data survives wait steps, application restarts, and redeployments.
- Each data type is identified by the `static abstract string Identifier` property of the `IAutomationProcessData` interface.
- Multiple data types can coexist within the same process -- they are stored in a dictionary keyed by their identifier.
- If two implementations declare the same identifier, they overwrite each other's data.
- All `DateTime` values are automatically serialized and deserialized as UTC.
- If stored data cannot be deserialized (e.g., after a schema change), `GetProcessData` returns `null` instead of throwing an exception.

### Define a process data class

Implement the `IAutomationProcessData` interface. The `Identifier` property must be a unique, stable string:

{% code lang=csharp title="CrmSyncData.cs" %}
using CMS.Automation;

public class CrmSyncData : IAutomationProcessData
{
    public static string Identifier => "Acme.CrmSyncData";

    public string CrmRecordId { get; set; }

    public bool SyncSucceeded { get; set; }
}
{% endcode %}

### Example: Two-step data sharing

The following example demonstrates a common pattern -- one action writes data and a subsequent action reads it.

**Step 1** -- Push contact to CRM and store the result:

{% code lang=csharp title="PushToCrmAction.cs" %}
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

[assembly: RegisterAutomationAction<PushToCrmAction>(
    identifier: "Acme_Crm_PushContact",
    displayName: "Push contact to CRM")]

namespace Acme.Automation;

public class PushToCrmAction : AutomationAction
{
    private readonly ICrmClient crmClient;

    public PushToCrmAction(ICrmClient crmClient)
    {
        this.crmClient = crmClient;
    }

    public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        ContactInfo contact = context.ProcessedObject as ContactInfo;
        if (contact is null)
        {
            return;
        }

        CrmResult result = await crmClient.UpsertContactAsync(contact, cancellationToken);

        // Store the result for downstream steps
        await context.SetProcessData(new CrmSyncData
        {
            CrmRecordId = result.RecordId,
            SyncSucceeded = result.Success
        }, cancellationToken);
    }
}
{% endcode %}

**Step 2** -- Read the CRM sync result from an earlier step:

{% code lang=csharp title="LogCrmResultAction.cs" %}
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;
using CMS.ContactManagement;

using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationAction<LogCrmResultAction>(
    identifier: "Acme_Crm_LogSyncResult",
    displayName: "Log CRM sync result")]

namespace Acme.Automation;

public class LogCrmResultAction : AutomationAction
{
    private readonly ILogger<LogCrmResultAction> logger;

    public LogCrmResultAction(ILogger<LogCrmResultAction> logger)
    {
        this.logger = logger;
    }

    public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        CrmSyncData syncData = await context.GetProcessData<CrmSyncData>(cancellationToken);

        if (syncData is null)
        {
            logger.LogWarning("No CRM sync data found in process context.");
            return;
        }

        if (syncData.SyncSucceeded)
        {
            logger.LogInformation("Contact synced to CRM record {RecordId}.", syncData.CrmRecordId);
        }
        else
        {
            ContactInfo contact = context.ProcessedObject as ContactInfo;
            logger.LogWarning("CRM sync failed for contact {ContactId}.", contact?.ContactID);
        }
    }
}
{% endcode %}

## Best practices

### Performance

- Keep the `Execute` method lightweight -- automation processes can run for a large number of contacts on high-traffic sites.
- Offload heavy I/O work (e.g., external API calls) to background queues when possible. Enqueue the work and return immediately.
- Cache configuration data that is safe to reuse (lookup tables, configuration values) using `IProgressiveCache` or a similar caching mechanism.

### Execution timeout

Each custom action has a maximum execution time of **2 minutes**. If the action does not complete within this window, a `TimeoutException` is thrown and the step fails for the given contact.

Design actions to complete quickly. For long-running operations, enqueue work to an external queue or background service and return from `Execute` immediately.

### Error handling

- Unhandled exceptions in `Execute` cause the automation step to fail for the given contact.
- Handle expected errors (API timeouts, network failures) gracefully and log them.
- Always respect the `CancellationToken` parameter -- the system cancels execution during application shutdown or when the timeout is reached.

{% code lang=csharp title="Error handling example" %}
public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
{
    try
    {
        await externalApi.CallAsync(cancellationToken);
    }
    catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
    {
        logger.LogError(ex, "External API call failed for contact {ContactId}.",
            (context.ProcessedObject as ContactInfo)?.ContactID);
        // The step completes successfully — the failure is logged but does not block the process
    }
}
{% endcode %}

### Data protection (GDPR)

- **Do not store personal data** (names, emails, phone numbers) in `IAutomationProcessData` implementations. The system does not currently provide a built-in mechanism to erase or export process data as part of data protection (right to erasure / right to portability) workflows.
- Store only non-personal identifiers (internal IDs, external record IDs, boolean flags, timestamps).
- If storing personal data in process context is absolutely necessary, create a dedicated data cleaner action and place it at the **end of every branch** in the automation process:

{% code lang=csharp title="CleanProcessDataAction.cs" %}
using System.Threading;
using System.Threading.Tasks;

using CMS.Automation;

[assembly: RegisterAutomationAction<CleanProcessDataAction>(
    identifier: "Acme_DataProtection_CleanProcessData",
    displayName: "Clean process data",
    Tooltip = "Removes custom process data for data protection compliance.")]

namespace Acme.Automation;

public class CleanProcessDataAction : AutomationAction
{
    public override async Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        // Overwrite each known process data type with a cleared instance
        await context.SetProcessData(new CrmSyncData
        {
            CrmRecordId = null,
            SyncSucceeded = false
        }, cancellationToken);

        // Add SetProcessData calls for other IAutomationProcessData types as needed
    }
}
{% endcode %}

### Updating and versioning actions

- **Never change the `identifier`** of a registered action -- doing so breaks all processes that reference the action.
- **Never rename the action class** that is used in enabled or running processes.
- Adding new optional properties to an `IAutomationActionProperties` class is safe -- they receive default values for existing step configurations.
- Removing or renaming properties breaks existing step configurations.

To version an action safely:

1. Create a new action class with a new identifier (e.g., `Acme_Crm_PushContact_V2`).
2. Keep the old version registered and functional.
3. Update automation processes to use the new version when convenient -- ideally when no contacts are currently in the old step.
4. Once all processes using the old version have completed (no contacts remain in any step of those processes), the old action class can safely be removed.

{% note %}
When disabling a process before making changes, remember that contacts already in the process (including those in Wait steps) will continue progressing through existing steps.
{% endnote %}

### Dependency injection

Actions are resolved from the DI container. Use constructor injection to obtain services:

{% code lang=csharp %}
public class MyAction : AutomationAction
{
    private readonly ILogger<MyAction> logger;
    private readonly ICrmClient crmClient;
    private readonly IProgressiveCache cache;

    public MyAction(ILogger<MyAction> logger, ICrmClient crmClient, IProgressiveCache cache)
    {
        this.logger = logger;
        this.crmClient = crmClient;
        this.cache = cache;
    }

    public override Task Execute(AutomationProcessContext context, CancellationToken cancellationToken)
    {
        // All injected services are available
        // ...
    }
}
{% endcode %}

## API reference

{% table %}
    {% row header=true %}
        {% cell %}
        Type
        {% endcell %}

        {% cell %}
        Namespace
        {% endcell %}

        {% cell %}
        Description
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `AutomationAction`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Base class for actions without configurable properties.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `AutomationAction<TProperties>`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Base class for actions with configurable properties.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `IAutomationActionProperties`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Marker interface for property model classes.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `IAutomationProcessData`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Interface for typed data shared across automation steps.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `AutomationProcessContext`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Runtime context providing access to the processed object and cross-step data.
        {% endcell %}
    {% endrow %}

    {% row %}
        {% cell %}
        `RegisterAutomationActionAttribute<TAction>`
        {% endcell %}

        {% cell %}
        `CMS.Automation`
        {% endcell %}

        {% cell %}
        Assembly attribute for registering custom actions.
        {% endcell %}
    {% endrow %}
{% endtable %}
