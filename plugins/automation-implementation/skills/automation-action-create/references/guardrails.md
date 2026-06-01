# Guardrails — team rules and design patterns

Rules that go beyond what the API documentation (`automation-customization.md`) says. Security policies, concurrency rules, and conventions specific to this codebase.

## Canonical patterns

### `ProcessedObject` guard

Every action that expects a contact must short-circuit when the processed object is not a `ContactInfo`. Use this exact shape:

```csharp
if (context.ProcessedObject is not ContactInfo contact)
{
    logger.LogWarning(
        "Skipping {ActionName} — processed object is not a contact (got '{ObjectType}').",
        nameof(MyAction),
        context.ProcessedObject?.TypeInfo.ObjectType);
    return;
}
```

Substitute the expected type if the action handles a different object type. Never assume the cast succeeds — `ProcessedObject` is `BaseInfo`.

## Concurrency

- Do not keep per-execution state in instance fields. The action class instance may be reused across executions, and `Execute` may run concurrently for different processed objects.
- Do not mutate static state from `Execute`.

## TProperties — no secrets

- **No secrets in `TProperties`.** API keys, OAuth secrets, signing keys, account SIDs — read them from `IConfiguration` / `IOptions<T>` inside the action. Properties shown in the configuration dialog are visible to anyone who can edit the automation process.

## Dependency injection

- Use **typed `HttpClient`** for outbound HTTP — register with `services.AddHttpClient<TAction>()`. Never `new HttpClient()` per call.

## External calls and idempotency

- When the action talks to an external system, key the call by a stable identifier derived from `context.ProcessedObject` (typically `ContactInfo.ContactID`) so retries — caused by failures or republishes — do not double-apply.

## Async

- Never `.Result` or `.Wait()` inside `Execute`. Use `await`.
- `Task.CompletedTask` only for genuinely synchronous bodies. Do not fake async with `Task.Run`.

## Logging

- Use `ILogger<T>` injected into the action. **Do not use `IEventLogService`.**
- Include `ContactInfo.ContactID` (or another identifier from `context.ProcessedObject`) in log scopes or messages so a single failing object can be traced.
- Do not log the property payload — it may contain webhook URLs, customer-supplied templates, or other sensitive data.

## Marketer experience

- Pick an icon (`xp-...` class) that matches the action's intent. Default is `xp-cogwheel`.
- `Tooltip` answers "what does this step do?" in one sentence.
- Use marketer-friendly labels on properties (`Webhook URL`, not `WebhookEndpoint`).
