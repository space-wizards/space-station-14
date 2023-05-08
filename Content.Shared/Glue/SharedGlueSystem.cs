using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DragDrop;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Emoting;
using Content.Shared.Movement;
using Content.Shared.Movement.Events;
using Robust.Shared.Serialization;

namespace Content.Shared.Glue
{
    public abstract class SharedGlueSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        public override void Initialize()
        {
        }
    }
}

