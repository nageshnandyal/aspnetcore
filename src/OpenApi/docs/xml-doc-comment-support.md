# XML Documentation Comment Support in Microsoft.AspNetCore.OpenApi

This document provides background context and a proposed solution around the problem of adding XML comment support to OpenAPI documents generated via `Microsoft.AspNetCore.OpenApi`.

## Background

C# source files leverage a structured comment schema that can be mapped to API documentation. During compilation, the C# compiler produces an XML file that contains this structured metadata and the types associated with the documented APIs. This structured file can be queried to resolve comments associated with types.

For example, the given source file:

```csharp
/// <summary>
/// Represents a todo item.
/// </summary>
public class TestClass
{
    public string? TestProperty { get; set; }
}
```

Will produce the following structured XML:

```xml
<?xml version="1.0"?>
<doc>
    <assembly>
        <name>Sample</name>
    </assembly>
    <members>
        <member name="T:TestClass">
            <summary>
            Represents a todo item.
            </summary>
        </member>
    </members>
</doc>
```

OpenAPI documents benefit from being able to source metadata from this generated file, including:

- Descriptions and summaries attached on types
- Types consumed as parameters and returned by methods
- Examples associated with types

To that end, there's a desire to integrate XML documentation with the generated OpenAPI document. The proposed solution should be:

- High-performance
- Native AoT-friendly
- Provide sensible mappings from XML to OpenAPI schema

## Proposed Solution

The proposed solution consists of three things:

- A source generator to support the discovery of types and their XML comments at build-time
- APIs source-shared from the DocFx library to support parsing of the XML structured document into an in-memory model
- A set of new APIs to support injecting the resolved XML comments at build-time via interception and facilitating lookup at runtime

For a given sample application:

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddOpenApi();
builder.Services.AddXmlServices();

var app = builder.Build();

app.MapPost("/todo", (Todo todo) => TypedResults.Ok(todo));

app.Run();

/// <summary>
/// Represents a todo item.
/// </summary>
class Todo
{
    /// <summary>
    /// Represents a unique identifier associated with the todo.
    /// </summary>
    public int Id { get; init; }
    /// <summary>
    /// Represents the title of the todo.
    /// </summary>
    public string Title { get; init; }
    /// <summary>
    /// Represents whether or not the task is completed.
    /// </summary>
    public bool IsCompleted { get; init; }
}
```

## XML to OpenAPI Schema Mappings

The following table provides mappings from XML definitions to the associated code comments.
