// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

public class OpenApiSchemaReferenceTransformerTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task IdenticalParameterTypesAreStoredWithSchemaReference()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IFormFile value) => { });
        builder.MapPost("/api-2", (IFormFile value) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var parameter = operation.RequestBody.Content["multipart/form-data"];
            var schema = parameter.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var parameter2 = operation2.RequestBody.Content["multipart/form-data"];
            var schema2 = parameter2.Schema;

            // {
            //   "$ref": "#/components/schemas/IFormFileValue"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "IFormFileValue": {
            //         "type": "object",
            //         "properties": {
            //           "value": {
            //             "$ref": "#/components/schemas/IFormFile"
            //           }
            //         }
            //       },
            //       "IFormFile": {
            //         "type": "string",
            //         "format": "binary"
            //       }
            //     }
            //   }
            Assert.Equal(schema.Reference, schema2.Reference);

            var effectiveSchema = schema.GetEffective(document);
            Assert.Equal("object", effectiveSchema.Type);
            Assert.Equal(1, effectiveSchema.Properties.Count);
            var effectivePropertySchema = effectiveSchema.Properties["value"].GetEffective(document);
            Assert.Equal("string", effectivePropertySchema.Type);
            Assert.Equal("binary", effectivePropertySchema.Format);
        });
    }

    [Fact]
    public async Task TodoInRequestBodyAndResponseUsesSchemaReference()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Todo todo) => TypedResults.Ok(todo));

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var response = operation.Responses["200"];
            var responseContent = response.Content["application/json"];
            var responseSchema = responseContent.Schema;

            // {
            //   "$ref": "#/components/schemas/Todo"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "Todo": {
            //         "type": "object",
            //         "properties": {
            //           "id": {
            //             "type": "integer"
            //           },
            //           ...
            //         }
            //       }
            //     }
            //   }
            Assert.Equal(requestBodySchema.Reference.Id, responseSchema.Reference.Id);

            var effectiveSchema = requestBodySchema.GetEffective(document);
            Assert.Equal("object", effectiveSchema.Type);
            Assert.Equal(4, effectiveSchema.Properties.Count);
            var effectiveIdSchema = effectiveSchema.Properties["id"].GetEffective(document);
            Assert.Equal("integer", effectiveIdSchema.Type);
            var effectiveTitleSchema = effectiveSchema.Properties["title"].GetEffective(document);
            Assert.Equal("string", effectiveTitleSchema.Type);
            var effectiveCompletedSchema = effectiveSchema.Properties["completed"].GetEffective(document);
            Assert.Equal("boolean", effectiveCompletedSchema.Type);
            var effectiveCreatedAtSchema = effectiveSchema.Properties["createdAt"].GetEffective(document);
            Assert.Equal("string", effectiveCreatedAtSchema.Type);
        });
    }

    [Fact]
    public async Task SameTypeInDictionaryAndListTypesUsesReferenceIds()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Todo[] todo) => { });
        builder.MapPost("/api-2", (Dictionary<string, Todo> todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema;

            // {
            //   "type": "array",
            //   "items": {
            //     "$ref": "#/components/schemas/Todo"
            //   }
            // }
            // {
            //   "type": "object",
            //   "additionalProperties": {
            //     "$ref": "#/components/schemas/Todo"
            //   }
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "Todo": {
            //         "type": "object",
            //         "properties": {
            //           "id": {
            //             "type": "integer"
            //           },
            //           ...
            //         }
            //       }
            //     }
            //   }
            // }

            // Parent types of schemas are different
            Assert.Equal("array", requestBodySchema.Type);
            Assert.Equal("object", requestBodySchema2.Type);
            // Values of the list and dictionary point to the same reference ID
            Assert.Equal(requestBodySchema.Items.Reference.Id, requestBodySchema2.AdditionalProperties.Reference.Id);
        });
    }

    [Fact]
    public async Task SameTypeInAllOfReferenceGetsHandledCorrectly()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IFormFile resume, [FromForm] Todo todo) => { });
        builder.MapPost("/api-2", ([FromForm] string name, [FromForm] Todo todo2) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["multipart/form-data"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["multipart/form-data"];
            var requestBodySchema2 = requestBody2.Schema;

            // Todo parameter (second parameter) in allOf for each operation should point to the same reference ID.
            Assert.Equal(requestBodySchema.AllOf[1].Reference.Id, requestBodySchema2.AllOf[1].Reference.Id);

            // IFormFile parameter should use inline schema since it only appears once in the application.
            Assert.Equal("object", requestBodySchema.AllOf[0].Type);
            Assert.Equal("string", requestBodySchema.AllOf[0].Properties["resume"].Type);
            Assert.Equal("binary", requestBodySchema.AllOf[0].Properties["resume"].Format);

            // String parameter `name` should use reference ID shared by string properties in the
            // Todo object.
            Assert.Equal("object", requestBodySchema2.AllOf[0].Type);
            var nameParameterReference = requestBodySchema2.AllOf[0].Properties["name"].Reference.Id;
            var todoTitleReference = requestBodySchema.AllOf[1].GetEffective(document).Properties["title"].Reference.Id;
            var todoTitleReference2 = requestBodySchema2.AllOf[1].GetEffective(document).Properties["title"].Reference.Id;
            Assert.Equal(nameParameterReference, todoTitleReference);
            Assert.Equal(nameParameterReference, todoTitleReference2);
        });
    }

    [Fact]
    public async Task DifferentTypesWithSameSchemaMapToSameReferenceId()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (IEnumerable<Todo> todo) => { });
        builder.MapPost("/api-2", (Todo[] todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody.Content["application/json"];
            var requestBodySchema = requestBody.Schema;

            var operation2 = document.Paths["/api-2"].Operations[OperationType.Post];
            var requestBody2 = operation2.RequestBody.Content["application/json"];
            var requestBodySchema2 = requestBody2.Schema;

            // {
            //  "$ref": "#/components/schemas/TodoArray"
            // }
            // {
            //  "$ref": "#/components/schemas/TodoArray"
            // }
            // {
            //   "components": {
            //     "schemas": {
            //       "TodoArray": {
            //         "type": "array",
            //         "items": {
            //           "$ref": "#/components/schemas/Todo"
            //         }
            //       }
            //     }
            //   }
            // }

            // Both list types should point to the same reference ID
            Assert.Equal(requestBodySchema.Reference.Id, requestBodySchema2.Reference.Id);
            // The referenced schema has an array type
            Assert.Equal("array", requestBodySchema.GetEffective(document).Type);
            // The items in the array are mapped to the Todo reference
            Assert.NotNull(requestBodySchema.GetEffective(document).Items.Reference.Id);
            Assert.Equal(4, requestBodySchema.GetEffective(document).Items.GetEffective(document).Properties.Count);
        });
    }
}
