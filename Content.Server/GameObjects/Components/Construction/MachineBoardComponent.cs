using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineBoardComponent : Component, IExamine
    {
        public override string Name => "MachineBoard";

        [ViewVariables] [YamlField("requirements")]
        private Dictionary<MachinePart, int> _requirements = new();

        [ViewVariables]
        [YamlField("materialRequirements")]
        private Dictionary<StackType, int> _materialRequirements = new();

        [ViewVariables]
        [YamlField("componentRequirements")]
        private Dictionary<string, ComponentPartInfo> _componentRequirements = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("prototype")]
        public string Prototype { get; private set; }
        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;
        public IReadOnlyDictionary<StackType, int> MaterialRequirements => _materialRequirements;
        public IReadOnlyDictionary<string, ComponentPartInfo> ComponentRequirements => _componentRequirements;

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
    public struct ComponentPartInfo : IDeepClone
    {
        public int Amount;
        public string ExamineName;
        public string DefaultPrototype;
        public IDeepClone DeepClone()
        {
            return new ComponentPartInfo()
            {
                Amount = Amount,
                ExamineName = ExamineName,
                DefaultPrototype = DefaultPrototype
            };
        }
    }
}
