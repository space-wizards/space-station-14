using Content.Shared.ActionBlocker;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.Server.Tabletop.Components
{
    /**
     * <summary>A component that makes an object playable as a tabletop game.</summary>
     */
    [RegisterComponent]
    public class TabletopGameComponent : Component
    {
        public override string Name => "TabletopGame";

        /**
         * <summary>A verb that allows the player to start playing a tabletop game.</summary>
         */
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
                // TODO: add icon
            }

            protected override void Activate(IEntity user, TabletopGameComponent component)
            {
                // Tell the client that it has to open a viewport for the tabletop game
                var entityNetManager = component.Owner.EntityManager.EntityNetManager;
                entityNetManager?.SendSystemNetworkMessage(new TabletopPlayEvent());
            }
        }
    }
}
