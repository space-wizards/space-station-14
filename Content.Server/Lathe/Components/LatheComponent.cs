using System.Threading.Tasks;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Server.Research.Components;
using Content.Server.Stack;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Server.Player;

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
        public float InsertionTime = 0.9f;

        public float InsertionAccumulator = 0f;
        [ViewVariables]
        public float ProducingAccumulator = 0f;
        [ViewVariables]
        public float? ProductionTime => ProducingRecipe?.CompleteTime / 1000;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(LatheUiKey.Key);
    }
}
