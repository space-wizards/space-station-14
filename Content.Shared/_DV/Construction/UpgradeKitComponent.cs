// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._DV.Construction;

/// <summary>
/// Component for an upgrade kit that upgrades allowed machines then deletes itself.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(UpgradeKitSystem))]
public sealed partial class UpgradeKitComponent : Component
{
    /// <summary>
    /// A whitelist that entities must match to be upgraded.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// A blacklist that entities cannot match to be upgraded.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Blacklist = new();

    /// <summary>
    /// Components added to the machine after it's upgraded.
    /// Some of these must blacklist it from upgrades to prevent stacking.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// How long the doafter is
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Sound played when upgrading an entity.
    /// </summary>
    [DataField]
    public SoundSpecifier? UpgradeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    public EntityUid? SoundStream;
}

[Serializable, NetSerializable]
public sealed partial class UpgradeKitDoAfterEvent : SimpleDoAfterEvent;
