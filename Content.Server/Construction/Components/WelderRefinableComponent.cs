using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tool;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Construction.Components
{
    /// <summary>
    /// Used for something that can be refined by welder.
    /// For example, glass shard can be refined to glass sheet.
    /// </summary>
    [RegisterComponent]
    public class WelderRefinableComponent : Component, IInteractUsing
    {
        [ViewVariables]
        [DataField("refineResult")]
        private HashSet<string>? _refineResult = new() { };
        [ViewVariables]
        [DataField("refineTime")]
        private float _refineTime = 2f;

        private bool _beingWelded;

        public override string Name => "WelderRefinable";

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // check if object is welder
            if (!eventArgs.Using.TryGetComponent(out ToolComponent? tool))
                return false;

            // check if someone is already welding object
            if (_beingWelded)
                return false;
            _beingWelded = true;

            if (!await tool.UseTool(eventArgs.User, Owner, _refineTime, ToolQuality.Welding))
            {
                // failed to veld - abort refine
                _beingWelded = false;
                return false;
            }

            // get last owner coordinates and delete it
            var resultPosition = Owner.Transform.Coordinates;
            Owner.Delete();

            // spawn each result after refine
            foreach (var result in _refineResult!)
            {
                var droppedEnt = Owner.EntityManager.SpawnEntity(result, resultPosition);

                // TODO: If something has a stack... Just use a prototype with a single thing in the stack.
                // This is not a good way to do it.
                if (droppedEnt.TryGetComponent<StackComponent>(out var stack))
                    EntitySystem.Get<StackSystem>().SetCount(droppedEnt.Uid, stack, 1);
            }

            return true;
        }
    }
}
