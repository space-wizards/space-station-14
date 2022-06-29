using Content.Shared.Interaction;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    public sealed class MorgueTrayComponent : Component
    {
        [ViewVariables]
        public EntityUid Morgue { get; set; }
        /*
        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (Morgue != default && !entMan.Deleted(Morgue) && entMan.TryGetComponent<MorgueEntityStorageComponent?>(Morgue, out var comp))
            {
                comp.Activate(new ActivateEventArgs(eventArgs.User, Morgue));
            }
        }*/
    }
}
