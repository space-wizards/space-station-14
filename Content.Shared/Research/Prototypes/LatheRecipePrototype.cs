using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Lathe.Prototypes;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes
{
    [NetSerializable, Serializable, Prototype]
    public sealed partial class LatheRecipePrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Name displayed in the lathe GUI.
        /// </summary>
        [DataField]
        public LocId? Name;

        /// <summary>
        ///     Short description displayed in the lathe GUI.
        /// </summary>
        [DataField]
        public LocId? Description;

        /// <summary>
        ///     The prototype name of the resulting entity when the recipe is printed.
        /// </summary>
        [DataField]
        public EntProtoId? Result;

        [DataField]
        public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>? ResultReagents;

        /// <summary>
        ///     An entity whose sprite is displayed in the ui in place of the actual recipe result.
        /// </summary>
        [DataField]
        public SpriteSpecifier? Icon;

        [DataField("completetime")]
        public TimeSpan CompleteTime = TimeSpan.FromSeconds(5);

        /// <summary>
        ///     The materials required to produce this recipe.
        ///     Takes a material ID as string.
        /// </summary>
        [DataField]
        public Dictionary<ProtoId<MaterialPrototype>, int> Materials = new();

        [DataField]
        public bool ApplyMaterialDiscount = true;

        /// <summary>
        /// A category used for visually sorting lathe recipes in the UI.
        /// </summary>
        [DataField]
        public ProtoId<LatheCategoryPrototype>? Category;
    }
}
