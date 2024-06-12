// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi.SourceGenerators;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

public partial class XmlCommentGeneratorTests
{
    [Fact]
    public async Task SmokeTest()
    {
        await new CSharpSourceGeneratorTest<XmlCommentGenerator, XUnitVerifier>
        {
            TestState =
            {
                Sources = { Sources[nameof(SmokeTest)].Input },
                GeneratedSources =
                {
                    (typeof(XmlCommentGenerator), "XmlCommentGenerator.CommentCache.generated.cs", Sources[nameof(SmokeTest)].CommentCache)
                }
            }
        }.RunAsync();
    }
}
