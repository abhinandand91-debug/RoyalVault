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
