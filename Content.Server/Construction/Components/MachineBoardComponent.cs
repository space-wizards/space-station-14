using System;
using System.Collections.Generic;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Construction.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public sealed class MachineBoardComponent : Component, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables]
        [DataField("requirements")]
        public readonly Dictionary<MachinePart, int> Requirements = new();

        [ViewVariables]
        [DataField("materialRequirements")]
        public readonly Dictionary<string, int> MaterialIdRequirements = new();

        [ViewVariables]
        [DataField("tagRequirements")]
        public readonly Dictionary<string, GenericPartInfo> TagRequirements = new();

        [ViewVariables]
        [DataField("componentRequirements")]
        public readonly Dictionary<string, GenericPartInfo> ComponentRequirements = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype")]
        public string? Prototype { get; private set; }

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

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("machine-board-component-on-examine-label") + "\n");
            foreach (var (part, amount) in Requirements)
            {
                message.AddMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", amount),
                                                ("requiredElement", Loc.GetString(part.ToString())))
                                  + "\n");
            }

            foreach (var (material, amount) in MaterialRequirements)
            {
                message.AddMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", amount),
                                                ("requiredElement", Loc.GetString(material.Name)))
                                  + "\n");
            }

            foreach (var (_, info) in ComponentRequirements)
            {
                message.AddMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", info.Amount),
                                                ("requiredElement", Loc.GetString(info.ExamineName)))
                                  + "\n");
            }

            foreach (var (_, info) in TagRequirements)
            {
                message.AddMarkup(Loc.GetString("machine-board-component-required-element-entry-text",
                                                ("amount", info.Amount),
                                                ("requiredElement", Loc.GetString(info.ExamineName)))
                                  + "\n");
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
