using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     This is simplified version of <see cref="HandheldLightComponent"/>.
    ///     It doesn't consume any power and can be toggle only by verb.
    /// </summary>
    [RegisterComponent]
    public class UnpoweredFlashlightComponent : Component
    {
        [DataField("toggleFlashlightSound")]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

        [ViewVariables] public bool LightOn = false;
    }
}
