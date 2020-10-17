#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Mechanism
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMechanismComponent))]
    [ComponentReference(typeof(IMechanism))]
    public class MechanismComponent : SharedMechanismComponent
    {
        protected override void OnAddedToPart()
        {
            base.OnAddedToPart();

            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.Visible = false;
            }
        }

        protected override void OnRemovedFromPart(IBodyPart old)
        {
            base.OnRemovedFromPart(old);

            if (Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                sprite.Visible = true;
            }
        }
    }
}
