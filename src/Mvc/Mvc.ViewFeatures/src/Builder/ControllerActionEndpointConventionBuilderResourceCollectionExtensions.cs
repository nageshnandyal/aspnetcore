// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="ControllerActionEndpointConventionBuilder"/>.
/// </summary>
public static class ControllerActionEndpointConventionBuilderResourceCollectionExtensions
{
    /// <summary>
    /// Adds a <see cref="ResourceAssetCollection"/> metadata instance to the endpoints.
    /// </summary>
    /// <param name="builder">The <see cref="ControllerActionEndpointConventionBuilder"/>.</param>
    /// <param name="manifestPath">The manifest associated with the assets.</param>
    /// <returns></returns>
    public static ControllerActionEndpointConventionBuilder WithResourceCollection(
        this ControllerActionEndpointConventionBuilder builder,
        string manifestPath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var endpointBuilder = builder.Items["__EndpointRouteBuilder"];
        var (resolver, registered) = builder.Items.TryGetValue("__ResourceCollectionResolver", out var value)
            ? ((ResourceCollectionResolver)value, true)
            : (new ResourceCollectionResolver((IEndpointRouteBuilder)endpointBuilder), false);

        resolver.ManifestName = manifestPath;
        if (!registered)
        {
            var collection = resolver.ResolveResourceCollection();
            var importMap = resolver.ResolveImportMap();

            builder.Add(endpointBuilder =>
            {
                // Do not add metadata to API controllers
                if (endpointBuilder.Metadata.OfType<ApiControllerAttribute>().Any())
                {
                    return;
                }

                endpointBuilder.Metadata.Add(collection);
                endpointBuilder.Metadata.Add(importMap);
            });
        }

        return builder;
    }
}
