using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Construction.Components
{
    /// <summary>
    /// Used for something that can be refined by welder.
    /// For example, glass shard can be refined to glass sheet.
    /// </summary>
    [RegisterComponent]
    public class WelderRefinableComponent : Component, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("refineResult")]
        private HashSet<string>? _refineResult = new() { };

        [DataField("refineTime")]
        private float _refineTime = 2f;

        [DataField("refineFuel")]
        private float _refineFuel = 0f;

        [DataField("qualityNeeded", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _qualityNeeded = "Welding";

        private bool _beingWelded;

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // check if object is welder
            if (!_entMan.TryGetComponent(eventArgs.Using, out ToolComponent? tool))
                return false;

            // check if someone is already welding object
            if (_beingWelded)
                return false;

            _beingWelded = true;

            var toolSystem = EntitySystem.Get<ToolSystem>();

            if (!await toolSystem.UseTool(eventArgs.Using, eventArgs.User, Owner, _refineFuel, _refineTime, _qualityNeeded))
            {
                // failed to veld - abort refine
                _beingWelded = false;
                return false;
            }

            // get last owner coordinates and delete it
            var resultPosition = _entMan.GetComponent<TransformComponent>(Owner).Coordinates;
            _entMan.DeleteEntity(Owner);

            // spawn each result after refine
            foreach (var result in _refineResult!)
            {
                var droppedEnt = _entMan.SpawnEntity(result, resultPosition);

                // TODO: If something has a stack... Just use a prototype with a single thing in the stack.
                // This is not a good way to do it.
                if (_entMan.TryGetComponent<StackComponent?>(droppedEnt, out var stack))
                    EntitySystem.Get<StackSystem>().SetCount(droppedEnt,1, stack);
            }

            return true;
        }
    }
}
