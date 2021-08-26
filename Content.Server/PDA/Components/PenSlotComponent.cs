using Content.Server.PDA.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA.Components
{
    [RegisterComponent]
    public class PenSlotComponent : Component
    {
        public override string Name => "PenSlot";

        [ViewVariables] [DataField("pen")] public string? StartingPen;
        [ViewVariables] public ContainerSlot PenSlot = default!;

        [Verb]
        public sealed class EjectPenVerb : Verb<PenSlotComponent>
        {
            protected override void GetData(IEntity user, PenSlotComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("eject-pen-verb-get-data-text");
                data.Visibility = component.PenSlot.ContainedEntity == null ? VerbVisibility.Invisible : VerbVisibility.Visible;
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PenSlotComponent component)
            {
                EntitySystem.Get<PenSlotSystem>().TryEjectContent(component, user);
            }
        }
    }
}
