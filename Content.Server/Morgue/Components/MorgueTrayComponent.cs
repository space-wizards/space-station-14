using Content.Shared.Interaction;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public sealed class MorgueTrayComponent : Component, IActivate
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
