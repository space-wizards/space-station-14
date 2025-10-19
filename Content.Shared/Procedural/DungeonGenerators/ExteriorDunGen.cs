// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Generates the specified config on an exterior tile of the attached dungeon.
/// Useful if you're using <see cref="GroupDunGen"/> or otherwise want a dungeon on the outside of a grid.
/// </summary>
public sealed partial class ExteriorDunGen : IDunGenLayer
{
    [DataField(required: true)]
    public ProtoId<DungeonConfigPrototype> Proto;
}
