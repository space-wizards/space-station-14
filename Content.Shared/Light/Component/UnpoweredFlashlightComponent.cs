using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Audio;

namespace Content.Shared.Light.Component;

/// <summary>
/// This is simplified version of <see cref="HandheldLightComponent"/>.
/// It doesn't consume any power and can be toggle only by verb.
/// </summary>
[RegisterComponent]
public sealed class UnpoweredFlashlightComponent : Robust.Shared.GameObjects.Component
{
    [DataField("toggleFlashlightSound")]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

    [ViewVariables] public bool LightOn = false;

    [DataField("toggleAction", required: true)]
    public InstantAction ToggleAction = new();
}
