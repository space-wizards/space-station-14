// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Numerics;
using Content.Shared.BloodCult.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.BloodCult.Systems;

/// <summary>
///     Ray emitted by cult healing source towards cultist.
///     Contains all information about encountered radiation blockers.
/// </summary>
[Serializable, NetSerializable]
public sealed class CultHealingRay
{
    /// <summary>
    ///     Map on which source and receiver are placed.
    /// </summary>
    public MapId MapId;
    /// <summary>
    ///     Uid of entity with <see cref="CultHealingSourceComponent"/>.
    /// </summary>
    public NetEntity SourceUid;
    /// <summary>
    ///     World coordinates of cult healing source.
    /// </summary>
    public Vector2 Source;
    /// <summary>
    ///     Uid of entity with <see cref="BloodCultistComponent"/>.
    /// </summary>
    public NetEntity DestinationUid;
    /// <summary>
    ///     World coordinates of health receiver.
    /// </summary>
    public Vector2 Destination;
    /// <summary>
    ///     How much healing intensity reached receiver.
    /// </summary>
    public float Healing;

    /// <summary>
    ///     Has healing ray reached destination or lost all intensity after blockers?
    /// </summary>
    public bool ReachedDestination => Healing > 0;

    /// <summary>
    ///     All blockers visited by gridcast. Key is uid of grid. Values are pairs
    ///     of tile indices and floats with updated radiation value.
    /// </summary>
    /// <remarks>
    ///     Last tile may have negative value if ray has lost all intensity.
    ///     Grid traversal order isn't guaranteed.
    /// </remarks>
    public Dictionary<NetEntity, List<(Vector2i, float)>> Blockers = new();

    public CultHealingRay(MapId mapId, NetEntity sourceUid, Vector2 source,
        NetEntity destinationUid, Vector2 destination, float healing)
    {
        MapId = mapId;
        SourceUid = sourceUid;
        Source = source;
        DestinationUid = destinationUid;
        Destination = destination;
        Healing = healing;
    }
}
