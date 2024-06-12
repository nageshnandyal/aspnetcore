// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

[Generator]
public sealed partial class XmlCommentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var comments = context.CompilationProvider.Select(ParseComments);
        var cacheResolutions = context.SyntaxProvider.CreateSyntaxProvider(FilterInvocations, TransformInvocations);

        var result = comments.Combine(cacheResolutions.Collect());

        context.RegisterSourceOutput(result, (context, output) =>
        {
            var comments = output.Left;
            var interceptableLocations = output.Right;
            EmitXmlCommentCache(context, comments, interceptableLocations);
        });
    }
}
