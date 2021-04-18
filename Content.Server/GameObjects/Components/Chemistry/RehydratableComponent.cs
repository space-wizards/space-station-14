#nullable enable
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Basically, monkey cubes.
    /// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a given reagent.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(ISolutionChange))]
    public class RehydratableComponent : Component, ISolutionChange
    {
        public override string Name => "Rehydratable";

        [ViewVariables]
        [DataField("catalyst")]
        private string _catalystPrototype = "Water";
        [ViewVariables]
        [DataField("target")]
        private string? _targetPrototype = default!;

        private bool _expanding;

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
