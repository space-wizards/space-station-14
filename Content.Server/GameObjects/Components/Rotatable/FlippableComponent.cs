#nullable enable
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Rotatable
{
    [RegisterComponent]
    public class FlippableComponent : Component
    {
        public override string Name => "Flippable";


        private string? _entity => _internalEntity ?? Owner.Prototype?.ID;

        [DataField("entity")]
        private string? _internalEntity;

        private void TryFlip(IEntity user)
        {
            if (Owner.TryGetComponent(out IPhysicsComponent? physics) &&
                physics.Anchored)
            {
                Owner.PopupMessage(user, Loc.GetString("It's stuck."));
                return;
            }

            if (_entity == null)
            {
                return;
            }

            Owner.EntityManager.SpawnEntity(_entity, Owner.Transform.Coordinates);
            Owner.Delete();
        }

        [Verb]
        private sealed class FlippableVerb : Verb<FlippableComponent>
        {
            protected override void GetData(IEntity user, FlippableComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Flip");
            }

            protected override void Activate(IEntity user, FlippableComponent component)
            {
                component.TryFlip(user);
            }
        }
    }
}
