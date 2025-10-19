// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Codewords;

/// <summary>
/// Container for generated codewords.
/// </summary>
[RegisterComponent, Access(typeof(CodewordSystem))]
public sealed partial class CodewordComponent : Component
{
    /// <summary>
    /// The codewords that were generated.
    /// </summary>
    [DataField]
    public string[] Codewords = [];
}
