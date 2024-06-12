// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi;

/// <summary>
/// Partial class for source generator-based XML documentation generation.
/// </summary>
public interface IXmlCommentService
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="key"></param>
    /// <param name="summary"></param>
    /// <returns></returns>
    bool TryGetXmlComment((Type, string?) key, out string? summary);
}
