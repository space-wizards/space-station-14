using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using System;

namespace Content.Shared.Light.Component;

/// <summary>
/// Networked ~~solely for admemes~~ for completely legitimate reasons, like hacked energy swords.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
[ComponentProtoName("RgbLightController")]
[Friend(typeof(SharedRgbLightControllerSystem))]
public sealed class RgbLightControllerComponent : Robust.Shared.GameObjects.Component
{
    [DataField("cycleRate")]
    public float CycleRate { get; set; } = 0.1f;

    /// <summary>
    ///     What layers of the sprite to modulate? If null, will affect the whole sprite.
    /// </summary>
    [DataField("layers")]
    public int[]? Layers;
}

[Serializable, NetSerializable]
public class RgbLightControllerState : ComponentState
{
    public readonly float CycleRate;
    public readonly int[]? Layers;

    public RgbLightControllerState(float cycleRate, int[]? layers)
    {
        CycleRate = cycleRate;
        Layers = layers;
    }
}
