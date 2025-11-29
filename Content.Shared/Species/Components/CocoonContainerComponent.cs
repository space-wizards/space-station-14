// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Containers.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Species.Arachnid;

/// <summary>
/// Component for the cocoon container entity that holds the victim.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CocoonContainerComponent : Component
{
    /// <summary>
    /// The entity that is cocooned inside this container.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Victim;

    /// <summary>
    /// Accumulated damage that the cocoon has absorbed. The cocoon breaks after reaching MaxDamage.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedDamage = 0f;

    /// <summary>
    /// Maximum damage the cocoon can absorb before breaking.
    /// </summary>
    [DataField]
    public float MaxDamage = 10f;

    /// <summary>
    /// Percentage of damage that the cocoon absorbs (0.3 = 30%).
    /// </summary>
    [DataField]
    public float AbsorbPercentage = 0.3f;
}

/// <summary>
/// Networked event sent from server to client to trigger the rotation animation.
/// </summary>
[Serializable, NetSerializable]
public sealed class CocoonRotationAnimationEvent(NetEntity cocoon, bool victimWasStanding) : EntityEventArgs
{
    public NetEntity Cocoon = cocoon;
    public bool VictimWasStanding = victimWasStanding;
}

