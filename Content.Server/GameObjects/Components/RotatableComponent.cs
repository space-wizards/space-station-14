using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RotatableComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        public override string Name => "Rotatable";

        private void TryRotate(IEntity user, Angle angle)
        {
            if (Owner.TryGetComponent(out PhysicsComponent physics))
            {
                if (physics.Anchored)
                {
                    _notifyManager.PopupMessage(Owner.Transform.GridPosition, user, _localizationManager.GetString("It's stuck."));
                    return;
                }
            }

            Owner.Transform.LocalRotation += angle;
        }

        [Verb]
        public sealed class RotateVerb : Verb<RotatableComponent>
        {
            protected override string GetText(IEntity user, RotatableComponent component)
            {
                return "Rotate clockwise";
            }

            protected override string GetCategory(IEntity user, RotatableComponent component) => "Rotate";

            protected override VerbVisibility GetVisibility(IEntity user, RotatableComponent component)
            {
                return VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, RotatableComponent component)
            {
                component.TryRotate(user, Angle.FromDegrees(-90));
            }
        }

        [Verb]
        public sealed class RotateCounterVerb : Verb<RotatableComponent>
        {
            protected override string GetText(IEntity user, RotatableComponent component)
            {
                return "Rotate counter-clockwise";
            }

            protected override string GetCategory(IEntity user, RotatableComponent component) => "Rotate";

            protected override VerbVisibility GetVisibility(IEntity user, RotatableComponent component)
            {
                return VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, RotatableComponent component)
            {
                component.TryRotate(user, Angle.FromDegrees(90));
            }
        }

    }
}
