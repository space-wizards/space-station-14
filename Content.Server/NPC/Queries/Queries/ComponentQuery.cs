// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

/// <summary>
/// Returns nearby components that match the specified components.
/// </summary>
public sealed partial class ComponentQuery : UtilityQuery
{
    [DataField("components", required: true)]
    public ComponentRegistry Components = default!;
}
