# Royal Vault

## C# Coding Standards – Mandatory

Follow traditional, clean, and consistent C# coding conventions throughout the entire Unity project.

### 1. Brace Style

- Always use Allman brace style.
- Opening braces `{` must be on a new line.
- Closing braces `}` must be on a separate line.
- Always use curly braces for `if`, `else`, `else if`, `for`, `foreach`, `while`, and other control flow statements.
- Never omit braces, even for single-line statements.

### 2. Statement Formatting

- Write only one statement per line.
- Never combine multiple statements on a single line.
- Keep method calls, assignments, declarations, and return statements on separate lines.
- Avoid overly compressed or condensed code.

### 3. Conditional Statements

Always write:

```csharp
if (condition)
{
    ExecuteAction();
}
else
{
    HandleAlternative();
}
```

Never write:

```csharp
if (condition) ExecuteAction();
```

### 4. Methods and Classes

- Use Allman style for class, method, property, and namespace declarations.
- Keep method bodies properly indented.
- Separate logical code sections with blank lines.
- Keep methods focused and readable.

### 5. General Guidelines

- Use 4 spaces for indentation.
- Avoid unnecessary one-line methods and expressions.
- Prefer readability and maintainability over fewer lines of code.
- Follow consistent naming conventions: PascalCase for classes and methods, camelCase for local variables and private fields.
- Do not reformat unrelated files unless explicitly requested.

### 6. Enforcement

These coding standards are mandatory for all newly generated or modified C# files.
Before completing a task, review the modified files and correct any formatting violations.

## Performance Standards – Mandatory

The game will keep growing, so every runtime feature is written to scale from the start.

### 1. Animation

- Use PrimeTween for all timed animation: fades, moves, scales, colours, delays and chains.
- Use a PrimeTween `Sequence` instead of a coroutine when the timing is fixed.
- Use a coroutine only when the code waits on the player or on game state (a tap, a reel landing, a scene load).
- Keep the `Tween` or `Sequence` handle in a field and call `Stop()` on it when the animation is cancelled.

### 2. No Allocations in Hot Paths

- Hot paths are `Update`, `LateUpdate`, coroutine loops, tween callbacks and anything that runs every spin.
- Do not create classes, arrays, lists or delegates there; reuse fields instead.
- Do not use LINQ there.
- Do not build strings there; update text only when its value changes.
- Tween callbacks must not capture locals or `this`: use the target overload, for example
  `Tween.Custom(this, 0f, 1f, duration, (view, value) => view.SetGlow(value))`.
- Cache `WaitForSeconds` and other yield instructions in fields.

### 3. Cached References

- Never call `GetComponent`, `Find`, `FindObjectOfType` or `Camera.main` in hot paths.
- Get references once in `Awake`, `Setup` or through serialized fields.

### 4. Object Pooling

- Pool anything spawned repeatedly (coins, particles, popups, symbols) instead of `Instantiate` and `Destroy`.

### 5. UI

- Turn off `raycastTarget` on every graphic that is not clickable.
- Put frequently changing UI (counters, effects) on its own nested `Canvas` so the whole screen does not rebuild.
- Avoid layout groups on animated content; position elements directly.
- Fade groups with `CanvasGroup.alpha` rather than toggling many objects.

### 6. Events and Lifetime

- Unsubscribe from events on longer-lived objects in `OnDestroy`.
- Stop tweens and coroutines that a view owns when it is disabled or destroyed.

### 7. Measure

- Profile with the Unity Profiler before micro-optimising; fix what the profiler shows first.
