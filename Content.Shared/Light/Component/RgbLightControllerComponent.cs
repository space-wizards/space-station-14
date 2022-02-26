using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using System;
using System.Collections.Generic;

namespace Content.Shared.Light.Component;

/// <summary>
/// Networked ~~solely for admemes~~ for completely legitimate reasons, like hacked energy swords.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
[Friend(typeof(SharedRgbLightControllerSystem))]
public sealed class RgbLightControllerComponent : Robust.Shared.GameObjects.Component
{
    [DataField("cycleRate")]
    public float CycleRate { get; set; } = 0.1f;

    /// <summary>
    ///     What layers of the sprite to modulate? If null, will affect the whole sprite.
    /// </summary>
    [DataField("layers")]
    public List<int>? Layers;

    // original colors when rgb was added. Used to revert Colors when removed.
    public Color OriginalLightColor;
    public Color OriginalItemColor;
    public Color OriginalSpriteColor;
    public Dictionary<int, Color>? OriginalLayerColors;
}

[Serializable, NetSerializable]
public sealed class RgbLightControllerState : ComponentState
{
    public readonly float CycleRate;
    public readonly List<int>? Layers;

    public RgbLightControllerState(float cycleRate, List<int>? layers)
    {
        CycleRate = cycleRate;
        Layers = layers;
    }
}
