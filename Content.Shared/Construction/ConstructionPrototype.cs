using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Prototype("construction")]
    public class ConstructionPrototype : IPrototype, IIndexedPrototype
    {
        public string Name => _name;
        private string _name;

        public string Description => _description;
        private string _description;

        public string Icon => _icon;
        private string _icon;

        public IReadOnlyList<string> Keywords => _keywords;
        private List<string> _keywords;

        public IReadOnlyList<string> CategorySegments => _categorySegments;
        private List<string> _categorySegments;

        public IReadOnlyList<(ConstructionStep forward, ConstructionStep reverse)> Steps => _steps;
        private List<(ConstructionStep forward, ConstructionStep reverse)> _steps
            = new List<(ConstructionStep forward, ConstructionStep reverse)>();

        private ConstructionType _type;

        public ConstructionType Type { get => _type; private set => _type = value; }

        private string _id;
        private string _result;

        public string ID { get => _id; private set => _id = value; }

        public string Result { get => _result; private set => _result = value; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);
            _name = ser.ReadDataField<string>("name");

            ser.DataField(ref _id, "id", string.Empty);
            ser.DataField(ref _description, "description", string.Empty);
            ser.DataField(ref _icon, "icon", string.Empty);
            ser.DataField(ref _type, "objecttype", ConstructionType.Structure);
            ser.DataField(ref _result, "result", null);

            _keywords = ser.ReadDataField<List<string>>("keywords", new List<string>());
            {
                var cat = ser.ReadDataField<string>("category");
                var split = cat.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                _categorySegments = split.ToList();
            }

            foreach (var stepMap in mapping.GetNode<YamlSequenceNode>("steps").Cast<YamlMappingNode>())
            {
                var step = ReadStepPrototype(stepMap);
                ConstructionStep reverse = null;

                if (stepMap.TryGetNode<YamlMappingNode>("reverse", out var reverseMap))
                {
                    reverse = ReadStepPrototype(reverseMap);
                }

                _steps.Add((step, reverse));
            }
        }

        ConstructionStep ReadStepPrototype(YamlMappingNode step)
        {
            int amount = 1;

            if (step.TryGetNode("amount", out var node))
            {
                amount = node.AsInt();
            }

            if (step.TryGetNode("material", out node))
            {
                return new ConstructionStepMaterial(
                    node.AsEnum<ConstructionStepMaterial.MaterialType>(),
                    amount
                );
            }

            if (step.TryGetNode("tool", out node))
            {
                return new ConstructionStepTool(
                    node.AsEnum<ConstructionStepTool.ToolType>(),
                    amount
                );
            }

            throw new InvalidOperationException("Not enough data specified to determine step.");
        }
    }

    public enum ConstructionType
    {
        Structure,
        Item,
    }

    public abstract class ConstructionStep
    {
        public readonly int Amount = 1;

        protected ConstructionStep(int amount)
        {
            Amount = amount;
        }
    }

    public class ConstructionStepTool : ConstructionStep
    {
        public readonly ToolType Tool;

        public ConstructionStepTool(ToolType tool, int amount) : base(amount)
        {
            Tool = tool;
        }

        public enum ToolType
        {
            Wrench,
            Welder,
            Screwdriver,
            Crowbar,
            Wirecutters,
        }
    }

    public class ConstructionStepMaterial : ConstructionStep
    {
        public readonly MaterialType Material;

        public ConstructionStepMaterial(MaterialType material, int amount) : base(amount)
        {
            Material = material;
        }


        public enum MaterialType
        {
            Metal,
            Glass,
            Cable,
        }
    }
}

