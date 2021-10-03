using Content.Server.Light.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
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
        public override string Name => "UnpoweredFlashlight";

        [DataField("toggleFlashlightSound")]
        public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");

        [ViewVariables] public bool LightOn = false;

        [Verb]
        public sealed class ToggleFlashlightVerb : Verb<UnpoweredFlashlightComponent>
        {
            protected override void GetData(IEntity user, UnpoweredFlashlightComponent component, VerbData data)
            {
                var canInteract = EntitySystem.Get<ActionBlockerSystem>().CanInteract(user);

                data.Visibility = canInteract ? VerbVisibility.Visible : VerbVisibility.Invisible;
                data.Text = Loc.GetString("toggle-flashlight-verb-get-data-text");
                data.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, UnpoweredFlashlightComponent component)
            {
                EntitySystem.Get<UnpoweredFlashlightSystem>().ToggleLight(component);
            }
        }
    }
}
