using Content.Server.GameObjects.EntitySystems.Click;
using Content.Shared.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    /// <summary>
    /// Holds data about an entity that is in the process of being constructed or destructed.
    /// </summary>
    [RegisterComponent]
    public class ConstructionComponent : Component, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _loc;
#pragma warning restore 649
        /// <inheritdoc />
        public override string Name => "Construction";

        /// <summary>
        /// The current construction recipe being used to build this entity.
        /// </summary>
        [ViewVariables]
        public ConstructionPrototype Prototype { get; set; }

        /// <summary>
        /// The current stage of construction.
        /// </summary>
        [ViewVariables]
        public int Stage { get; set; }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("prototype", null,
                value => Prototype = value, () => Prototype);

            serializer.DataReadWriteFunction("stage", 0,
                value => Stage = value, () => Stage);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            var stages = Prototype.Stages;
            if (Stage > 0 && Stage < stages.Count)
            {
                var curStage = stages[Stage];
                if (curStage.Backward != null && curStage.Backward is ConstructionStepTool)
                {
                    var backward = (ConstructionStepTool) curStage.Backward;
                    message.AddText(_loc.GetString("To deconstruct: {0}x {1} Tool", backward.Amount, backward.ToolQuality));
                }
                if (curStage.Forward != null && curStage.Forward is ConstructionStepMaterial)
                {
                    if (curStage.Backward != null)
                    {
                        message.AddText("\n");
                    }
                    var forward = (ConstructionStepMaterial) curStage.Forward;
                    message.AddText(_loc.GetString("To construct: {0}x {1}", forward.Amount, forward.Material));
                }
            }
        }
    }
}
