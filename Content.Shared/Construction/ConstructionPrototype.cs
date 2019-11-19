using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Prototype("construction")]
    public class ConstructionPrototype : IPrototype, IIndexedPrototype
    {
#pragma warning disable CS0649
        [Dependency] readonly IPrototypeManager PrototypeManager;
#pragma warning restore
        private string _name;
        private string _description;
        private SpriteSpecifier _icon;
        private List<string> _keywords;
        private List<string> _categorySegments;
        private List<ConstructionStage> _stages = new List<ConstructionStage>();
        private ConstructionType _type;
        private string _id;
        private string _result;
        private string _placementMode;

        /// <summary>
        ///     Friendly name displayed in the construction GUI.
        /// </summary>
        public string Name => _name;

        /// <summary>
        ///     "Useful" description displayed in the construction GUI.
        /// </summary>
        public string Description => _description;

        /// <summary>
        ///     Texture path inside the construction GUI.
        /// </summary>
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     A list of keywords that are used for searching.
        /// </summary>
        public IReadOnlyList<string> Keywords => _keywords;

        /// <summary>
        ///     The split up segments of the category.
        /// </summary>
        public IReadOnlyList<string> CategorySegments => _categorySegments;

        /// <summary>
        ///     The list of stages of construction.
        ///     Construction is separated into "stages" which is basically a state in a linear FSM.
        ///     The stage has forward and optionally backwards "steps" which are the criteria to move around in the FSM.
        ///     NOTE that the stages are mapped differently than they appear in the prototype.
        ///     In the prototype, the forward step is displayed as "to move into this stage" and reverse "to move out of"
        ///     Stage 0 is considered "construction not started" and last stage is considered "construction is finished".
        ///     As such, neither last or 0 stage have actual stage DATA, only backward/forward steps respectively.
        ///     This would be akward for a YAML prototype because it's always jagged.
        /// </summary>
        public IReadOnlyList<ConstructionStage> Stages => _stages;

        public ConstructionType Type => _type;

        public string ID => _id;

        /// <summary>
        ///     The prototype name of the entity prototype when construction is done.
        /// </summary>
        public string Result => _result;

        public string PlacementMode => _placementMode;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);
            _name = ser.ReadDataField<string>("name");

            ser.DataField(ref _id, "id", string.Empty);
            ser.DataField(ref _description, "description", string.Empty);
            ser.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            ser.DataField(ref _type, "objecttype", ConstructionType.Structure);
            ser.DataField(ref _result, "result", null);
            ser.DataField(ref _placementMode, "placementmode", "PlaceFree");

            _keywords = ser.ReadDataField<List<string>>("keywords", new List<string>());
            {
                var cat = ser.ReadDataField<string>("category");
                var split = cat.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                _categorySegments = split.ToList();
            }

            {
                SpriteSpecifier nextIcon = null;
                ConstructionStep nextBackward = null;

                foreach (var stepMap in mapping.GetNode<YamlSequenceNode>("steps").Cast<YamlMappingNode>())
                {
                    var step = ReadStepPrototype(stepMap);
                    _stages.Add(new ConstructionStage(step, nextIcon, nextBackward)); //For forward steps.
                    if (stepMap.TryGetNode("icon", out var node))
                    {
                        nextIcon = SpriteSpecifier.FromYaml(node);
                    }
                    else
                    {
                        nextIcon = _icon;
                    }
                    if (stepMap.TryGetNode("reverse", out YamlMappingNode revMap))
                    {
                        nextBackward = ReadStepPrototype(revMap);
                    }
                }

                _stages.Add(new ConstructionStage(null, nextIcon, nextBackward)); //For backward steps.
            }
        }

        ConstructionStep ReadStepPrototype(YamlMappingNode step)
        {
            int amount = 1;
            string audioClip = "/audio/items/deconstruct.ogg";
            if (step.TryGetNode("amount", out var node))
            {
                amount = node.AsInt();
            }
            if (step.TryGetNode("audio", out node))
            {
                audioClip = node.AsString();
            }
            if(step.TryGetNode("entityID", out node))
            {
                
                return new ConstructionStepEntityPrototype(node.AsString(), amount, audioClip);
            }

            throw new InvalidOperationException("Not enough data specified to determine step.");
        }
    }

    public sealed class ConstructionStage
    {
        /// <summary>
        ///     The icon of the construction frame at this stage.
        /// </summary>
        public readonly SpriteSpecifier Icon;

        /// <summary>
        ///     The step that should be completed to move away from this stage to the next one.
        /// </summary>
        public readonly ConstructionStep Forward;

        /// <summary>
        ///     The optional step that can be completed to move away from this stage to the previous one.
        /// </summary>
        public readonly ConstructionStep Backward;

        public ConstructionStage(ConstructionStep forward, SpriteSpecifier icon = null, ConstructionStep backward = null)
        {
            Icon = icon;
            Forward = forward;
            Backward = backward;
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
        public readonly string AudioClip = "/audio/items/deconstruct.ogg";

        protected ConstructionStep(int amount, string audioClip)
        {
            Amount = amount;
            AudioClip = audioClip;
        }
    }

    public class ConstructionStepEntityPrototype : ConstructionStep //Let's make construction steps just generic entities instead of hardcoding special cases. That'd be silly.
    {
        public readonly string EntityID;

        public ConstructionStepEntityPrototype(string entity, int amount, string audioClip) : base(amount, audioClip)
        {
            EntityID = entity;
        }
    }
}

