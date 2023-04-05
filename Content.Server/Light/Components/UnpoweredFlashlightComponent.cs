using Content.Shared.Actions.ActionTypes;
using Content.Shared.Light;
using Robust.Shared.Audio;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     This is simplified version of <see cref="HandheldLightComponent"/>.
    ///     It doesn't consume any power and can be toggle only by verb.
    /// </summary>
    [RegisterComponent]
    public sealed class UnpoweredFlashlightComponent : Component
    {
        [DataField("toggleFlashlightSound")]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

        [ViewVariables] public bool LightOn = false;

        [DataField("toggleAction", required: true)]
        public InstantAction ToggleAction = new();
    }
}
