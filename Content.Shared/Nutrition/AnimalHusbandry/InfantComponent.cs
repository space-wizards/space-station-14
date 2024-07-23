﻿using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.AnimalHusbandry;

/// <summary>
/// This is used for marking entities as infants.
/// Infants have half the size, visually, and cannot breed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class InfantComponent : Component
{
    /// <summary>
    /// How long the entity remains an infant.
    /// </summary>
    [DataField("infantDuration")]
    public TimeSpan InfantDuration = TimeSpan.FromMinutes(3);

    /// <summary>
    /// The base scale of the entity
    /// </summary>
    [DataField("defaultScale")]
    public Vector2 DefaultScale = Vector2.One;

    /// <summary>
    /// The size difference of the entity while it's an infant.
    /// </summary>
    [DataField("visualScale")]
    public Vector2 VisualScale = new(.5f, .5f);

    /// <summary>
    /// When the entity will stop being an infant.
    /// </summary>
    [DataField("infantEndTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan InfantEndTime;
}
