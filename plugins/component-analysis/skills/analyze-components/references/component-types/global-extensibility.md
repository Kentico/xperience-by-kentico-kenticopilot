# Global extensibility category guidance

## In scope

- global event handlers
- scheduled tasks
- custom modules and initialization hooks
- object type extensions
- provider customizations
- other project-wide supported extension points discovered during the audit

## Discovery anchors

- module initialization classes
- scheduled task implementations and registration points
- provider customization classes
- global event subscriptions
- service registration patterns that support global customizations

## Consistency priorities

- supported extension points are used consistently instead of ad-hoc workarounds
- file, folder, and class naming follows one repeatable convention across similar extension types
- global event handlers remain minimal, deterministic, and side-effect aware
- scheduled tasks are implemented with repeatable idempotent patterns
- provider customizations and global hooks avoid hidden coupling
- cross-cutting customizations are organized into coherent module boundaries

## Evidence to capture

- registration and initialization code locations
- file paths and class names for equivalent extension points to verify naming convention alignment
- repeated patterns for event subscription and task execution
- examples of side effects, guard clauses, and observability
- cases where similar customizations use different extension points

## Common high-value findings

- scattered initialization logic across unrelated assemblies or folders
- scheduled tasks with inconsistent observability or retry safety patterns
- provider customizations implemented differently for comparable concerns
- hidden coupling between event handlers, services, and global hooks

## Recommendation style

Favor a small set of explicit, supported project-wide extension patterns that an agent can safely reproduce without guessing.

---

## Platform-specific checks (sourced from Kentico docs)

### Scheduled tasks — class and registration

- **`IScheduledTask` interface**: the task class must implement `IScheduledTask` with the `Execute(ScheduledTaskConfigurationInfo task, CancellationToken cancellationToken)` method. Missing interface implementation means the class cannot be registered.
- **`RegisterScheduledTask(identifier, typeof(Type))` assembly attribute**: required for the task to appear in the Scheduled tasks application. Only registered tasks can be configured. Check that all task classes have a corresponding registration attribute.
- **Identifier uniqueness and stability**: identifiers link the code class to its configuration in the Scheduled tasks application. Renaming an identifier for an already-configured task causes the task to error on its next run — the configuration must be updated manually. Check that identifiers use a stable `CompanyName.TaskName` prefix pattern.
- **Return value consistency**: `Execute()` must return a `ScheduledTaskExecutionResult`. On success, `ScheduledTaskExecutionResult.Success` (empty string result) is fine for tasks where no diagnostic output is needed. For tasks that can fail, a meaningful descriptive string should be returned to help troubleshoot via the Scheduled tasks application "Last result" column. Flag tasks that always return `Success` regardless of outcome.
- **Constructor DI is supported**: services can be injected via the constructor. Flag any tasks that use service locator patterns (`IServiceProvider` resolution) instead of constructor injection.
- **No request context in `Execute`**: scheduled tasks run in a dedicated worker thread and have no access to HTTP request context. Flag any tasks that attempt to access `IHttpContextAccessor` or request-scoped services.
- **Multi-instance awareness**: in multi-instance environments, a task is assigned to one instance nondeterministically. Tasks that assume they run on a specific instance or that share state across instances need careful review.
- **Scheduler reliability**: the scheduler runs within the web application process. If the app process is suspended (e.g., IIS idle timeout), tasks will not execute. Projects using IIS should set the application pool `Start Mode` to `AlwaysRunning` or equivalent.

### Assembly discoverability

- **`[assembly: AssemblyDiscoverable]` in custom class libraries**: required for Xperience to process assembly attributes (`RegisterScheduledTask`, `RegisterModule`, `RegisterImplementation`, etc.) in a custom class library. Without it, all registration attributes in that library are silently ignored.
- **Not required in the main web project**: `AddKentico()` in `Program.cs` handles discoverability for the main project automatically.
- **Check all custom class libraries**: verify each custom class library that contains registration attributes has `AssemblyDiscoverable`. The recommended approach is a dedicated `AssemblyAttributes.cs` file with `[assembly: AssemblyDiscoverable]`.

### Custom code organization

- **Separate class library projects for custom code**: the recommended pattern is to add custom classes to a separate Class Library project (assembly) referenced by the main web project, not directly to the web project. This provides cleaner separation and reusability. Flag projects that add custom extension code directly into the web project without a clear rationale.
- **NuGet package version alignment**: custom class libraries that reference Kentico NuGet packages must use the same version as the main `Kentico.Xperience.WebApp` package. Version mismatches cause runtime errors.

### Dependency injection and service registration

- **Services registered via `IServiceCollection` in `Program.cs`**: all Xperience services are added via `AddKentico()`. Custom services should be registered using standard ASP.NET Core DI patterns (`AddScoped`, `AddSingleton`, `AddTransient`). Check for consistency in lifetime choices — mismatched lifetimes (e.g., `Singleton` depending on `Scoped`) cause runtime errors.
- **`RegisterImplementation` attribute**: used to replace or extend Xperience service implementations. Check that custom provider/implementation registrations use the correct attribute and that the target interface is correct.
- **No `Task.Run` for database access without `CMSConnectionScope`**: parallel or async work scheduled from external libraries must use `ContextUtils.ResetCurrent()` (or `ContextUtils.PropagateCurrent()`) to ensure each worker thread gets a fresh database connection context. Shared connection context across threads causes `MultipleActiveResultSets` and related exceptions.

### Localization resources

- **`RegisterLocalizationResource(typeof(ResourceClass), target, culture)` assembly attribute**: registers `.resx` files for use by Xperience. The `target` parameter (`LocalizationTarget.Server`, `LocalizationTarget.Client`, or `LocalizationTarget.Builder`) must match where the strings are consumed.
- **Resource key prefix consistency**: keys should use a consistent company/project prefix (e.g., `acme.admin.label.createButton`) to avoid conflicts with system resources. Flag resources with generic un-prefixed keys.
- **Retrieval via `ILocalizationService`**: custom code should retrieve resource strings via `ILocalizationService.GetString()`. Flag any hardcoded UI strings in components that should be localized.
