// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

public sealed partial class ComponentFilter : UtilityQueryFilter
{
    /// <summary>
    /// Components to filter for.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// If true, this filter retains entities with ALL of the specified components. If false, this filter removes
    /// entities with ANY of the specified components.
    /// </summary>
    [DataField]
    public bool RetainWithComp = true;
}
