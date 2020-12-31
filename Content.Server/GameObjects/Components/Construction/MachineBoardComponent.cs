using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineBoardComponent : Component, IExamine
    {
        public override string Name => "MachineBoard";

        [ViewVariables]
        private Dictionary<MachinePart, int> _requirements;

        [ViewVariables]
        private Dictionary<StackType, int> _materialRequirements;

        [ViewVariables]
        private Dictionary<string, ComponentPartInfo> _componentRequirements;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Prototype { get; private set; }
        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;
        public IReadOnlyDictionary<StackType, int> MaterialRequirements => _materialRequirements;
        public IReadOnlyDictionary<string, ComponentPartInfo> ComponentRequirements => _componentRequirements;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Prototype, "prototype", null);
            serializer.DataField(ref _requirements, "requirements", new Dictionary<MachinePart, int>());
            serializer.DataField(ref _materialRequirements, "materialRequirements", new Dictionary<StackType, int>());
            serializer.DataField(ref _componentRequirements, "componentRequirements", new Dictionary<string, ComponentPartInfo>());
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Requires:\n"));
            foreach (var (part, amount) in Requirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(part.ToString())));
            }

            foreach (var (material, amount) in MaterialRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(material.ToString())));
            }

            foreach (var (_, info) in ComponentRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", info.Amount, Loc.GetString(info.ExamineName)));
            }
        }
    }

    [Serializable]
    public struct ComponentPartInfo
    {
        public int Amount;
        public string ExamineName;
        public string DefaultPrototype;
    }
}
