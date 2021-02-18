#nullable enable
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Basically, monkey cubes.
    /// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a given reagent.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IReagentReaction))]
    [ComponentReference(typeof(ISolutionChange))]
    public class RehydratableComponent : Component, IReagentReaction, ISolutionChange
    {
        public override string Name => "Rehydratable";

        [ViewVariables]
        private string _catalystPrototype = "";
        [ViewVariables]
        private string? _targetPrototype;

        private bool _expanding;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _catalystPrototype, "catalyst", "chem.Water");
            serializer.DataField(ref _targetPrototype, "target", null);
        }

        ReagentUnit IReagentReaction.ReagentReactTouch(ReagentPrototype reagent, ReagentUnit volume) => Reaction(reagent, volume);
        ReagentUnit IReagentReaction.ReagentReactInjection(ReagentPrototype reagent, ReagentUnit volume) => Reaction(reagent, volume);

        private ReagentUnit Reaction(ReagentPrototype reagent, ReagentUnit volume)
        {
            if ((volume > ReagentUnit.Zero) && (reagent.ID == _catalystPrototype))
            {
                Expand();
            }
            return ReagentUnit.Zero;
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            var solution = eventArgs.Owner.GetComponent<SolutionContainerComponent>();
            if (solution.Solution.GetReagentQuantity(_catalystPrototype) > ReagentUnit.Zero)
            {
                Expand();
            }
        }

        // Try not to make this public if you can help it.
        private void Expand()
        {
            if (_expanding)
            {
                return;
            }
            _expanding = true;
            Owner.PopupMessageEveryone(Loc.GetString("{0:TheName} expands!", Owner));
            if (!string.IsNullOrEmpty(_targetPrototype))
            {
                var ent = Owner.EntityManager.SpawnEntity(_targetPrototype, Owner.Transform.Coordinates);
                ent.Transform.AttachToGridOrMap();
            }
            Owner.Delete();
        }
    }
}
