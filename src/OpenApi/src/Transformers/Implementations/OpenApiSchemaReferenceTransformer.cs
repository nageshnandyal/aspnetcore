// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Document transformer to support mapping duplicate JSON schema instances
/// into JSON schema references across the document.
/// </summary>
internal sealed class OpenApiSchemaReferenceTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var schemaStore = context.ApplicationServices.GetRequiredKeyedService<OpenApiSchemaStore>(context.DocumentName);
        var schemasByReference = schemaStore.SchemasByReference;

        foreach (var pathItem in document.Paths.Values)
        {
            for (var i = 0; i < OpenApiConstants.OperationTypes.Length; i++)
            {
                var operationType = OpenApiConstants.OperationTypes[i];
                if (pathItem.Operations.TryGetValue(operationType, out var operation))
                {
                    if (operation.Parameters is not null)
                    {
                        foreach (var parameter in operation.Parameters)
                        {
                            parameter.Schema = ResolveReferenceForSchema(parameter.Schema, schemasByReference);
                        }
                    }

                    if (operation.RequestBody is not null)
                    {
                        foreach (var content in operation.RequestBody.Content)
                        {
                            content.Value.Schema = ResolveReferenceForSchema(content.Value.Schema, schemasByReference);
                        }
                    }

                    if (operation.Responses is not null)
                    {
                        foreach (var response in operation.Responses.Values)
                        {
                            if (response.Content is not null)
                            {
                                foreach (var content in response.Content)
                                {
                                    content.Value.Schema = ResolveReferenceForSchema(content.Value.Schema, schemasByReference);
                                }
                            }
                        }
                    }
                }
            }
        }

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        foreach (var (schema, referenceId) in schemasByReference.OrderBy(kvp => kvp.Value))
        {
            if (referenceId is not null)
            {
                document.Components.Schemas[referenceId] = ResolveReferenceForSchema(schema, schemasByReference, skipResolution: true);
            }
        }

        return Task.CompletedTask;
    }

    internal static OpenApiSchema? ResolveReferenceForSchema(OpenApiSchema? schema, ConcurrentDictionary<OpenApiSchema, string?> schemasByReference, bool skipResolution = false)
    {
        if (schema is null)
        {
            return schema;
        }

        if (!skipResolution && schemasByReference.TryGetValue(schema, out var referenceId) && referenceId is not null)
        {
            return new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = referenceId } };
        }

        if (schema.AllOf != null)
        {
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                schema.AllOf[i] = ResolveReferenceForSchema(schema.AllOf[i], schemasByReference);
            }
        }

        if (schema.OneOf != null)
        {
            for (var i = 0; i < schema.OneOf.Count; i++)
            {
                schema.OneOf[i] = ResolveReferenceForSchema(schema.OneOf[i], schemasByReference);
            }
        }

        if (schema.AnyOf != null)
        {
            for (var i = 0; i < schema.AnyOf.Count; i++)
            {
                schema.AnyOf[i] = ResolveReferenceForSchema(schema.AnyOf[i], schemasByReference);
            }
        }

        if (schema.AdditionalProperties is not null)
        {
            schema.AdditionalProperties = ResolveReferenceForSchema(schema.AdditionalProperties, schemasByReference);
        }

        if (schema.Items is not null)
        {
            schema.Items = ResolveReferenceForSchema(schema.Items, schemasByReference);
        }

        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                schema.Properties[property.Key] = ResolveReferenceForSchema(property.Value, schemasByReference);
            }
        }

        if (schema.Not is not null)
        {
            schema.Not = ResolveReferenceForSchema(schema.Not, schemasByReference);
        }

        return schema;
    }
}
