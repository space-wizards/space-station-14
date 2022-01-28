using Content.Shared.Singularity.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.Client.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedContainmentFieldComponent))]
    public class ContainmentFieldComponent : SharedContainmentFieldComponent
    {
        // Jesus what is this code.
        // Singulo cleanup WHEEENNN
        private SpriteComponent? _spriteComponent;

        protected override void Initialize()
        {
            base.Initialize();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out _spriteComponent))
            {
                Logger.Error($"{nameof(ContainmentFieldComponent)} created without {nameof(SpriteComponent)}");
            }
            else
            {
                _spriteComponent.NoRotation = true;
            }
        }
    }
}
