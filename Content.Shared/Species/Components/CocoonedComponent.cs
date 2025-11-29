// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Species.Arachnid;

[RegisterComponent, NetworkedComponent]
public sealed partial class CocoonedComponent : Component
{
    [DataField]
    public SpriteSpecifier Sprite = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Effects/cocooned.rsi"), "cocooned");

    /// <summary>
    ///     Accumulated damage that the cocoon has absorbed. The cocoon breaks after reaching 10 damage.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedDamage = 0f;

    /// <summary>
    ///     Maximum damage the cocoon can absorb before breaking.
    /// </summary>
    [DataField]
    public float MaxDamage = 10f;

    /// <summary>
    ///     Percentage of damage that the cocoon absorbs (0.3 = 30%).
    /// </summary>
    [DataField]
    public float AbsorbPercentage = 0.3f;

    [DataField]
    public ProtoId<AlertPrototype> CocoonedAlert = "Cocooned";
}

public sealed partial class RemoveCocoonAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public enum CocoonedKey
{
    Key
}
