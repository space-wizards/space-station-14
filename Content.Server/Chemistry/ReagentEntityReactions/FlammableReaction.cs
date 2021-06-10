using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Chemistry;
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
    public class FlammableReaction : ReagentEntityReaction
    {
        [DataField("reagents", true, customTypeSerializer:typeof(PrototypeIdHashSetSerializer<ReagentPrototype>))]
        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly HashSet<string> _reagents = new ();

        protected override void React(IEntity entity, ReagentPrototype reagent, ReagentUnit volume, Solution? source)
        {
            if (!entity.TryGetComponent(out FlammableComponent? flammable) || !_reagents.Contains(reagent.ID)) return;

            flammable.AdjustFireStacks(volume.Float() / 10f);
            source?.RemoveReagent(reagent.ID, volume);
        }
    }
}
