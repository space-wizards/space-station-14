using System.Threading.Tasks;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Server.GameObjects.Components.Culinary
{
    [RegisterComponent]
    class SliceableFoodComponent : Component, IInteractUsing, IExamine
    {
        public override string Name => "SliceableFood";

        int IInteractUsing.Priority => 1; // take priority over eating with utensils

        [ViewVariables(VVAccess.ReadWrite)] private string _slice;
        private ushort _totalCount;
        [ViewVariables(VVAccess.ReadWrite)] private string _sound;

        [ViewVariables(VVAccess.ReadWrite)] public ushort Count;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _slice, "slice", string.Empty);
            serializer.DataField(ref _sound, "sound", "/Audio/Items/Culinary/chop.ogg");
            serializer.DataField<ushort>(ref _totalCount, "count", 5);
        }

        public override void Initialize()
        {
            base.Initialize();
            Count = _totalCount;
            Owner.EnsureComponent<FoodComponent>();
            Owner.EnsureComponent<SolutionContainerComponent>();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(_slice))
            {
                return false;
            }
            if (!Owner.TryGetComponent(out SolutionContainerComponent solution))
            {
                return false;
            }
            if (!eventArgs.Using.TryGetComponent(out UtensilComponent utensil) || !utensil.HasType(UtensilType.Knife))
            {
                return false;
            }

            var itemToSpawn = Owner.EntityManager.SpawnEntity(_slice, Owner.Transform.Coordinates);
            if (eventArgs.User.TryGetComponent(out HandsComponent handsComponent))
            {
                if (ContainerHelpers.IsInContainer(Owner))
                {
                    handsComponent.PutInHandOrDrop(itemToSpawn.GetComponent<ItemComponent>());
                }
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords(_sound, Owner.Transform.Coordinates,
                AudioParams.Default.WithVolume(-2));

            Count--;
            if (Count < 1)
            {
                Owner.Delete();
                return true;
            }
            solution.TryRemoveReagent("chem.Nutriment", solution.CurrentVolume / ReagentUnit.New(Count + 1));
            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString($"There are { Count } slices remaining."));
        }
    }
}
