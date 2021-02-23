using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Morgue
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class MorgueTrayComponent : Component, IActivate
    {
        public override string Name => "MorgueTray";

        [ViewVariables]
        public IEntity Morgue { get; set; }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(Morgue != null && !Morgue.Deleted && Morgue.TryGetComponent<MorgueEntityStorageComponent>(out var comp))
            {
                comp.Activate(new ActivateEventArgs()
                {
                    User = eventArgs.User,
                    Target = Morgue
                });
            }
        }
    }
}
