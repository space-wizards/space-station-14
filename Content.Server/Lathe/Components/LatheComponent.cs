
using Content.Server.UserInterface;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Sound;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    public sealed class LatheComponent : SharedLatheComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        public int VolumePerSheet = 100;
        [ViewVariables]
        public Queue<LatheRecipePrototype> Queue { get; } = new();
        [ViewVariables]
        public LatheRecipePrototype? ProducingRecipe;
        [ViewVariables]
        public float InsertionTime = 0.79f; // 0.01 off for animation timing
        public float InsertionAccumulator = 0f;
        [ViewVariables]
        public float ProducingAccumulator = 0f;
        [ViewVariables]
        public float? ProductionTime => ProducingRecipe?.CompleteTime / 1000;

        [DataField("producingSound")]
        public SoundSpecifier? ProducingSound;
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(LatheUiKey.Key);
    }
}
