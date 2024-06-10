// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Runtime.Interop;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

public class OpenApiSchemaComparerTests
{
    public static object[][] SinglePropertyData => [
        [new OpenApiSchema { Title = "Title" }, new OpenApiSchema { Title = "Title" }, true],
        [new OpenApiSchema { Title = "Title" }, new OpenApiSchema { Title = "Another Title" }, false],
        [new OpenApiSchema { Type = "string" }, new OpenApiSchema { Type = "string" }, true],
        [new OpenApiSchema { Type = "string" }, new OpenApiSchema { Type = "integer" }, false],
        [new OpenApiSchema { Format = "int32" }, new OpenApiSchema { Format = "int32" }, true],
        [new OpenApiSchema { Format = "int32" }, new OpenApiSchema { Format = "int64" }, false],
        [new OpenApiSchema { Maximum = 10 }, new OpenApiSchema { Maximum = 10 }, true],
        [new OpenApiSchema { Maximum = 10 }, new OpenApiSchema { Maximum = 20 }, false],
        [new OpenApiSchema { Minimum = 10 }, new OpenApiSchema { Minimum = 10 }, true],
        [new OpenApiSchema { Minimum = 10 }, new OpenApiSchema { Minimum = 20 }, false],
        [new OpenApiSchema { ExclusiveMaximum = true }, new OpenApiSchema { ExclusiveMaximum = true }, true],
        [new OpenApiSchema { ExclusiveMaximum = true }, new OpenApiSchema { ExclusiveMaximum = false }, false],
        [new OpenApiSchema { ExclusiveMinimum = true }, new OpenApiSchema { ExclusiveMinimum = true }, true],
        [new OpenApiSchema { ExclusiveMinimum = true }, new OpenApiSchema { ExclusiveMinimum = false }, false],
        [new OpenApiSchema { MaxLength = 10 }, new OpenApiSchema { MaxLength = 10 }, true],
        [new OpenApiSchema { MaxLength = 10 }, new OpenApiSchema { MaxLength = 20 }, false],
        [new OpenApiSchema { MinLength = 10 }, new OpenApiSchema { MinLength = 10 }, true],
        [new OpenApiSchema { MinLength = 10 }, new OpenApiSchema { MinLength = 20 }, false],
        [new OpenApiSchema { Pattern = "pattern" }, new OpenApiSchema { Pattern = "pattern" }, true],
        [new OpenApiSchema { Pattern = "pattern" }, new OpenApiSchema { Pattern = "another pattern" }, false],
        [new OpenApiSchema { MaxItems = 10 }, new OpenApiSchema { MaxItems = 10 }, true],
        [new OpenApiSchema { MaxItems = 10 }, new OpenApiSchema { MaxItems = 20 }, false],
        [new OpenApiSchema { MinItems = 10 }, new OpenApiSchema { MinItems = 10 }, true],
        [new OpenApiSchema { MinItems = 10 }, new OpenApiSchema { MinItems = 20 }, false],
        [new OpenApiSchema { UniqueItems = true }, new OpenApiSchema { UniqueItems = true }, true],
        [new OpenApiSchema { UniqueItems = true }, new OpenApiSchema { UniqueItems = false }, false],
        [new OpenApiSchema { MaxProperties = 10 }, new OpenApiSchema { MaxProperties = 10 }, true],
        [new OpenApiSchema { MaxProperties = 10 }, new OpenApiSchema { MaxProperties = 20 }, false],
        [new OpenApiSchema { MinProperties = 10 }, new OpenApiSchema { MinProperties = 10 }, true],
        [new OpenApiSchema { MinProperties = 10 }, new OpenApiSchema { MinProperties = 20 }, false],
        [new OpenApiSchema { Required = new HashSet<string>() { "required" } }, new OpenApiSchema { Required = new HashSet<string> { "required" } }, true],
        [new OpenApiSchema { Required = new HashSet<string>() { "name", "age" } }, new OpenApiSchema { Required = new HashSet<string> { "age", "name" } }, true],
        [new OpenApiSchema { Required = new HashSet<string>() { "required" } }, new OpenApiSchema { Required = new HashSet<string> { "another required" } }, false],
        [new OpenApiSchema { Enum = [new OpenApiString("value")] }, new OpenApiSchema { Enum = [new OpenApiString("value")] }, true],
        [new OpenApiSchema { Enum = [new OpenApiString("value")] }, new OpenApiSchema { Enum = [new OpenApiString("value2" )] }, false],
        [new OpenApiSchema { Enum = [new OpenApiString("value"), new OpenApiString("value2")] }, new OpenApiSchema { Enum = [new OpenApiString("value2" ), new OpenApiString("value" )] }, false],
        [new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { Items = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Items = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, true],
        [new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "string" } } }, new OpenApiSchema { Properties = new Dictionary<string, OpenApiSchema> { ["name"] = new OpenApiSchema { Type = "integer" } } }, false],
        [new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { AdditionalProperties = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { Description = "Description" }, new OpenApiSchema { Description = "Description" }, true],
        [new OpenApiSchema { Description = "Description" }, new OpenApiSchema { Description = "Another Description" }, false],
        [new OpenApiSchema { Deprecated = true }, new OpenApiSchema { Deprecated = true }, true],
        [new OpenApiSchema { Deprecated = true }, new OpenApiSchema { Deprecated = false }, false],
        [new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, true],
        [new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Description" } }, new OpenApiSchema { ExternalDocs = new OpenApiExternalDocs { Description = "Another Description" } }, false],
        [new OpenApiSchema { UnresolvedReference = true }, new OpenApiSchema { UnresolvedReference = true }, true],
        [new OpenApiSchema { UnresolvedReference = true }, new OpenApiSchema { UnresolvedReference = false }, false],
        [new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, true],
        [new OpenApiSchema { Reference = new OpenApiReference { Id = "Id", Type = ReferenceType.Schema } }, new OpenApiSchema { Reference = new OpenApiReference { Id = "Another Id", Type = ReferenceType.Schema } }, false],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, true],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("another value") } }, false],
        [new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key"] = new OpenApiString("value") } }, new OpenApiSchema { Extensions = new Dictionary<string, IOpenApiExtension> { ["key2"] = new OpenApiString("value") } }, false],
        [new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, true],
        [new OpenApiSchema { Xml = new OpenApiXml { Name = "Name" } }, new OpenApiSchema { Xml = new OpenApiXml { Name = "Another Name" } }, false],
        [new OpenApiSchema { Nullable = true }, new OpenApiSchema { Nullable = true }, true],
        [new OpenApiSchema { Nullable = true }, new OpenApiSchema { Nullable = false }, false],
        [new OpenApiSchema { ReadOnly = true }, new OpenApiSchema { ReadOnly = true }, true],
        [new OpenApiSchema { ReadOnly = true }, new OpenApiSchema { ReadOnly = false }, false],
        [new OpenApiSchema { WriteOnly = true }, new OpenApiSchema { WriteOnly = true }, true],
        [new OpenApiSchema { WriteOnly = true }, new OpenApiSchema { WriteOnly = false }, false],
        [new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, true],
        [new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "PropertyName" } }, new OpenApiSchema { Discriminator = new OpenApiDiscriminator { PropertyName = "AnotherPropertyName" } }, false],
        [new OpenApiSchema { Example = new OpenApiString("example") }, new OpenApiSchema { Example = new OpenApiString("example") }, true],
        [new OpenApiSchema { Example = new OpenApiString("example") }, new OpenApiSchema { Example = new OpenApiString("another example") }, false],
        [new OpenApiSchema { Example = new OpenApiInteger(2) }, new OpenApiSchema { Example = new OpenApiString("another example") }, false],
        [new OpenApiSchema { AdditionalPropertiesAllowed = true }, new OpenApiSchema { AdditionalPropertiesAllowed = true }, true],
        [new OpenApiSchema { AdditionalPropertiesAllowed = true }, new OpenApiSchema { AdditionalPropertiesAllowed = false }, false],
        [new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, true],
        [new OpenApiSchema { Not = new OpenApiSchema { Type = "string" } }, new OpenApiSchema { Not = new OpenApiSchema { Type = "integer" } }, false],
        [new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AnyOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { AllOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, true],
        [new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "string" }] }, new OpenApiSchema { OneOf = [new OpenApiSchema { Type = "integer" }] }, false],
        [new OpenApiSchema { MultipleOf = 10 }, new OpenApiSchema { MultipleOf = 10 }, true],
        [new OpenApiSchema { MultipleOf = 10 }, new OpenApiSchema { MultipleOf = 20 }, false],
        [new OpenApiSchema { Default = new OpenApiString("default") }, new OpenApiSchema { Default = new OpenApiString("default") }, true],
        [new OpenApiSchema { Default = new OpenApiString("default") }, new OpenApiSchema { Default = new OpenApiString("another default") }, false],
    ];

    [Theory]
    [MemberData(nameof(SinglePropertyData))]
    public void SinglePropertyProducesCorrectHashCode(OpenApiSchema schema, OpenApiSchema anotherSchema, bool isEqual)
    {
        // Act
        var hash = OpenApiSchemaComparer.Instance.GetHashCode(schema);
        var anotherHash = OpenApiSchemaComparer.Instance.GetHashCode(anotherSchema);

        // Assert
        Assert.Equal(isEqual, hash.Equals(anotherHash));
    }
}
