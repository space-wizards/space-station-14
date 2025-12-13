// SPDX-FileCopyrightText: 2025 Drywink <43855731+Drywink@users.noreply.github.com>
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
    /// Percentage of damage that the cocoon absorbs (0.3 = 30%).
    /// The absorbed damage is stored in the DamageableComponent, and the DestructibleComponent
    /// threshold determines when the cocoon breaks.
    /// </summary>
    [DataField]
    public float AbsorbPercentage = 0.3f;

    /// <summary>
    /// Flag to prevent recursion when processing damage changes.
    /// This is set to true while we're modifying damage to avoid infinite loops.
    /// </summary>
    [ViewVariables]
    public bool ProcessingDamage = false;
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

