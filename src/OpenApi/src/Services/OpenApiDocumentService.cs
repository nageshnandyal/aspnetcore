// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi;

internal sealed class OpenApiDocumentService(
    [ServiceKey] string documentName,
    IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
    IHostEnvironment hostEnvironment,
    IOptionsSnapshot<OpenApiOptions> optionsSnapshot)
{
    private readonly OpenApiOptions _options = optionsSnapshot.Get(documentName);

    // For good hygiene, operation-level tags must also appear in the document-level
    // tags collection. This set captures all tags that have been seen so far.
    // Note: internal for testing.
    internal readonly HashSet<OpenApiTag> _capturedTags = new(OpenApiTagComparer.Instance);

    public Task<OpenApiDocument> GetOpenApiDocumentAsync()
    {
        var document = new OpenApiDocument
        {
            Info = GetOpenApiInfo(),
            Paths = GetOpenApiPaths(),
            Tags = [.. _capturedTags]
        };
        return Task.FromResult(document);
    }

    internal OpenApiInfo GetOpenApiInfo()
    {
        return new OpenApiInfo
        {
            Title = $"{hostEnvironment.ApplicationName} | {documentName}",
            Version = OpenApiConstants.DefaultOpenApiVersion
        };
    }

    /// <summary>
    /// Gets the OpenApiPaths for the document based on the ApiDescriptions.
    /// </summary>
    /// <remarks>
    /// At this point in the construction of the OpenAPI document, we run
    /// each API description through the `ShouldInclude` delegate defined in
    /// the object to support filtering each
    /// description instance into its appropriate document.
    /// </remarks>
    internal OpenApiPaths GetOpenApiPaths()
    {
        var descriptionsByPath = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(_options.ShouldInclude)
            .GroupBy(apiDescription => apiDescription.MapRelativePathToItemPath());
        var paths = new OpenApiPaths();
        foreach (var descriptions in descriptionsByPath)
        {
            Debug.Assert(descriptions.Key != null, "Relative path mapped to OpenApiPath key cannot be null.");
            paths.Add(descriptions.Key, new OpenApiPathItem { Operations = GetOperations(descriptions) });
        }
        return paths;
    }

    internal Dictionary<OperationType, OpenApiOperation> GetOperations(IGrouping<string?, ApiDescription> descriptions)
    {
        var operations = new Dictionary<OperationType, OpenApiOperation>();
        foreach (var description in descriptions)
        {
            operations[description.ToOperationType()] = GetOperation(description);
        }
        return operations;
    }

    internal OpenApiOperation GetOperation(ApiDescription description)
    {
        var tags = GetTags(description);
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                _capturedTags.Add(tag);
            }
        }
        var operation = new OpenApiOperation
        {
            Summary = GetSummary(description),
            Description = GetDescription(description),
            Tags = tags,
        };
        return operation;
    }

    private static string? GetSummary(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointSummaryMetadata>().LastOrDefault()?.Summary;

    private static string? GetDescription(ApiDescription description)
        => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointDescriptionMetadata>().LastOrDefault()?.Description;

    private static List<OpenApiTag>? GetTags(ApiDescription description)
    {
        var actionDescriptor = description.ActionDescriptor;
        if (actionDescriptor.EndpointMetadata?.LastOrDefault(m => m is ITagsMetadata) is ITagsMetadata tagsMetadata)
        {
            return tagsMetadata.Tags.Select(tag => new OpenApiTag { Name = tag }).ToList();
        }
        return [new OpenApiTag { Name = description.ActionDescriptor.RouteValues["controller"] }];
    }
}