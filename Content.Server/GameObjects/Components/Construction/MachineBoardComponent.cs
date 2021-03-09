using System;
using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineBoardComponent : Component, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "MachineBoard";

        [ViewVariables]
        [DataField("requirements")]
        private Dictionary<MachinePart, int> _requirements = new();

        [ViewVariables]
        [DataField("materialRequirements")]
        private Dictionary<string, int> _materialIdRequirements = new();

        [ViewVariables]
        [DataField("tagRequirements")]
        private Dictionary<string, GenericPartInfo> _tagRequirements = new();

        [ViewVariables]
        [DataField("componentRequirements")]
        private Dictionary<string, GenericPartInfo> _componentRequirements = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype")]
        public string Prototype { get; private set; }

        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;

        public IReadOnlyDictionary<string, int> MaterialIdRequirements => _materialIdRequirements;

        public IEnumerable<KeyValuePair<StackPrototype, int>> MaterialRequirements
        {
            get
            {
                foreach (var (materialId, amount) in MaterialIdRequirements)
                {
                    var material = _prototypeManager.Index<StackPrototype>(materialId);
                    yield return new KeyValuePair<StackPrototype, int>(material, amount);
                }
            }
        }

        public IReadOnlyDictionary<string, GenericPartInfo> ComponentRequirements => _componentRequirements;
        public IReadOnlyDictionary<string, GenericPartInfo> TagRequirements => _tagRequirements;


        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Requires:\n"));
            foreach (var (part, amount) in Requirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(part.ToString())));
            }

            foreach (var (material, amount) in MaterialRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", amount, Loc.GetString(material.Name)));
            }

            foreach (var (_, info) in ComponentRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", info.Amount, Loc.GetString(info.ExamineName)));
            }

            foreach (var (_, info) in TagRequirements)
            {
                message.AddMarkup(Loc.GetString("[color=yellow]{0}x[/color] [color=green]{1}[/color]\n", info.Amount, Loc.GetString(info.ExamineName)));
            }
        }
    }

    [Serializable]
    [DataDefinition]
    public struct GenericPartInfo
    {
        [DataField("Amount")]
        public int Amount;
        [DataField("ExamineName")]
        public string ExamineName;
        [DataField("DefaultPrototype")]
        public string DefaultPrototype;
    }
}
