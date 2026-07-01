# Code quality guardrails

Rules that go beyond what the official Xperience API documentation says. Security, concurrency, idempotency, and house-style conventions for custom automation actions.

## Canonical patterns

### Retrieving the processed contact

Get the processed contact through the `GetProcessedObject` extension method (in `CMS.ContactManagement`), which returns the `ContactInfo` being processed:

```csharp
ContactInfo contact = await context.GetProcessedObject(cancellationToken);
```

`GetProcessedObject` throws `InvalidOperationException` when the processed object is not a `ContactInfo`. Do not read `context.ProcessedObject` directly — it is not part of the public API — and do not hand-roll a cast or guard. Custom actions placed in contact-based automation processes always receive a `ContactInfo`.

### One class per file

Each action class, its `TProperties` class, each `IAutomationProcessData` implementation, and any typed `*Options` class go in their own file named after the class. Do not co-locate the properties class in the action's file.

## Concurrency

- Do not keep per-execution state in instance fields. The action class instance may be reused across executions, and `Execute` may run concurrently for different processed objects.
- Do not mutate static state from `Execute`.

## TProperties — no secrets

- **No secrets in `TProperties`.** API keys, OAuth secrets, signing keys, account SIDs — read them from `IConfiguration` / `IOptions<T>` inside the action. Properties shown in the configuration dialog are visible to anyone who can edit the automation process.

## Dependency injection

- Use **typed `HttpClient`** for outbound HTTP — register with `services.AddHttpClient<TAction>()`. Never `new HttpClient()` per call.

## External calls and idempotency

- When the action talks to an external system, key the call by a stable identifier derived from the contact so retries — caused by failures or republishes — do not double-apply. `ContactInfo.ContactID` is local to the database; use `ContactInfo.ContactGUID` when the external system needs a globally stable key.

## Async

- Never `.Result` or `.Wait()` inside `Execute`. Use `await`.
- `Task.CompletedTask` only for genuinely synchronous bodies. Do not fake async with `Task.Run`.

## Logging

- Use `ILogger<T>` injected into the action. **Do not use `IEventLogService`.**
- Include `ContactInfo.ContactID` (or another identifier from the processed contact) in log scopes or messages so a single failing object can be traced.
- Do not log the property payload — it may contain webhook URLs, customer-supplied templates, or other sensitive data.

## Marketer experience

- Pick a specific icon constant from `Kentico.Xperience.Admin.Base.Icons` (e.g. `Icons.Bell`) that matches the action's intent — only fall back to `Icons.Cogwheel` if nothing better fits.
- `Description` answers "what does this step do?" in one sentence — it is the hover text shown in the Automation Builder step selector.
- Use marketer-friendly labels on properties (`Webhook URL`, not `WebhookEndpoint`).
