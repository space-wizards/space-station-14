#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Body.Digestive;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Basically, monkey cubes.
    /// But specifically, this component deletes the entity and spawns in a new entity when the entity is exposed to a given reagent.
    /// TODO: This needs support for reaction-on-touch, but that's waiting on merge of Zumorica's work on interactions between sprays and entities
    /// </summary>
    [RegisterComponent]
    public class RehydratableComponent : Component, ISolutionChange
    {
        public override string Name => "Rehydratable";

        [ViewVariables]
        private string _catalystPrototype = "";
        [ViewVariables]
        private string? _targetPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _catalystPrototype, "catalyst", "chem.H2O");
            serializer.DataField(ref _targetPrototype, "target", null);
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            var solution = eventArgs.Owner.GetComponent<SolutionContainerComponent>();
            if (solution.Solution.GetReagentQuantity(_catalystPrototype) > ReagentUnit.Zero)
            {
                Owner.PopupMessageEveryone(Loc.GetString("{0:TheName} expands!", Owner));
                if (!string.IsNullOrEmpty(_targetPrototype))
                {
                    Owner.EntityManager.SpawnEntity(_targetPrototype, Owner.Transform.Coordinates);
                }
                Owner.Delete();
            }
        }
    }
}
