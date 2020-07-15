using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Rotatable
{
    [RegisterComponent]
    public class FlippableComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Flippable";

        private void TryFlip(IEntity user)
        {
            if (Owner.TryGetComponent(out PhysicsComponent physics) &&
                physics.Anchored)
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user, Loc.GetString("It's stuck."));
                return;
            }

            Owner.Transform.LocalRotation += Angle.FromDegrees(180);
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
