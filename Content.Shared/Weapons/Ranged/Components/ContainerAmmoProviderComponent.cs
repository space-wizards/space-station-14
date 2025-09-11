﻿using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Handles pulling entities from the given container to use as ammunition.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedGunSystem))]
public sealed partial class ContainerAmmoProviderComponent : AmmoProviderComponent
{
    [DataField("container", required: true)]
    [ViewVariables]
    public string Container = default!;

    [DataField("provider")]
    [ViewVariables]
    public EntityUid? ProviderUid;
}
