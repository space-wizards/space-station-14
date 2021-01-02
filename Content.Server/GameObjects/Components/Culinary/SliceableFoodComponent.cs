using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.Components.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Culinary
{
    [RegisterComponent]
    class SliceableFoodComponent : Component, IInteractUsing, IExamine
    {
        public override string Name => "SliceableFood";

        [ViewVariables(VVAccess.ReadWrite)] private string _slice;
        private uint _totalCount;

        [ViewVariables(VVAccess.ReadWrite)] public uint Count;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _slice, "slice", "FoodOrangeCakeSlice");
            serializer.DataField<uint>(ref _totalCount, "count", 5);
        }
        
        public override void Initialize()
        {
            base.Initialize();
            Count = _totalCount;
            Owner.EnsureComponent<FoodComponent>();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent solution))
            {
                return false;
            }
            if (!eventArgs.Using.HasComponent<CulinarySharpComponent>())
            {
                return false;
            }

            Owner.EntityManager.SpawnEntity(_slice, Owner.Transform.Coordinates);

            Count--;
            solution.TryRemoveReagent("chem.Nutriment", solution.CurrentVolume / ReagentUnit.New(Count));
            if (Count < 1)
            {
                Owner.Delete();
            }
            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString($"There are { Count } slices remaining."));
        }
    }
}
