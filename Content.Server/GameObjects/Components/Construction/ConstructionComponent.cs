using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
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
            EntitySystem.Get<SharedConstructionSystem>().DoExamine(message, Prototype, Stage, inDetailsRange);
        }
    }
}
