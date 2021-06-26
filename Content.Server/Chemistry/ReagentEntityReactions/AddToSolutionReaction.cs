using System.Collections.Generic;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Chemistry.ReagentEntityReactions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AddToSolutionReaction : ReagentEntityReaction
    {
        [DataField("reagents", true, customTypeSerializer:typeof(PrototypeIdHashSetSerializer<ReagentPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly HashSet<string> _reagents = new ();

        protected override void React(IEntity entity, ReagentPrototype reagent, ReagentUnit volume, Solution? source)
        {
            if (!entity.TryGetComponent(out SolutionContainerComponent? solutionContainer) || (_reagents.Count > 0 && !_reagents.Contains(reagent.ID))) return;

            if(solutionContainer.TryAddReagent(reagent.ID, volume, out var accepted))
                source?.RemoveReagent(reagent.ID, accepted);
        }
    }
}
