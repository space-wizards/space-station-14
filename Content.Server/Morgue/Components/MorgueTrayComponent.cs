using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MorgueTrayComponent : Component, IActivate
    {
        [ViewVariables]
        public EntityUid Morgue { get; set; }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (Morgue != default && !entMan.Deleted(Morgue) && entMan.TryGetComponent<MorgueEntityStorageComponent?>(Morgue, out var comp))
            {
                comp.Activate(new ActivateEventArgs(eventArgs.User, Morgue));
            }
        }
    }
}
