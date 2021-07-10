using Content.Shared.ActionBlocker;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.Server.Tabletop.Components
{
    [RegisterComponent]
    public class TabletopGameComponent : Component
    {
        public override string Name => "TabletopGame";

        [Verb]
        public sealed class PlayVerb : Verb<TabletopGameComponent>
        {
            protected override void GetData(IEntity user, TabletopGameComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                // TODO: use localisation
                data.Text = "Play Game";
            }

            protected override void Activate(IEntity user, TabletopGameComponent component)
            {
                var eventBus = component.Owner.EntityManager.EventBus;
                eventBus.RaiseEvent(EventSource.Network, new TabletopPlayEvent());

                
            }
        }
    }
}
