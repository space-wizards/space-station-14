// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

/// <summary>
/// Represents a network of multiple tasks. This gets expanded out to its relevant nodes.
/// </summary>
[Prototype("htnCompound")]
public sealed partial class HTNCompoundPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    [DataField("branches", required: true)]
    public List<HTNBranch> Branches = new();

    /// <summary>
    /// Exclude this compound task from the CompoundRecursion integration test.
    /// </summary>
    [DataField]
    public bool AllowRecursion = false;
}
