using System.Threading.Tasks;
using Content.Server.Chemistry.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    class SliceableFoodComponent : Component, IInteractUsing, IExamine
    {
        public override string Name => "SliceableFood";

        int IInteractUsing.Priority => 1; // take priority over eating with utensils

        [DataField("slice")] [ViewVariables(VVAccess.ReadWrite)]
        private string _slice = string.Empty;

        [DataField("sound")] [ViewVariables(VVAccess.ReadWrite)]
        private string _sound = "/Audio/Items/Culinary/chop.ogg";

        [DataField("count")] [ViewVariables(VVAccess.ReadWrite)]
        private ushort _totalCount = 5;

        [ViewVariables(VVAccess.ReadWrite)] public ushort Count;

        protected override void Initialize()
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
            if (!Owner.TryGetComponent(out SolutionContainerComponent? solution))
            {
                return false;
            }
            if (!eventArgs.Using.TryGetComponent(out UtensilComponent? utensil) || !utensil.HasType(UtensilType.Knife))
            {
                return false;
            }

            var itemToSpawn = Owner.EntityManager.SpawnEntity(_slice, Owner.Transform.Coordinates);
            if (eventArgs.User.TryGetComponent(out HandsComponent? handsComponent))
            {
                if (ContainerHelpers.IsInContainer(Owner))
                {
                    handsComponent.PutInHandOrDrop(itemToSpawn.GetComponent<ItemComponent>());
                }
            }

            SoundSystem.Play(Filter.Pvs(Owner), _sound, Owner.Transform.Coordinates,
                AudioParams.Default.WithVolume(-2));

            Count--;
            if (Count < 1)
            {
                Owner.Delete();
                return true;
            }
            solution.TryRemoveReagent("Nutriment", solution.CurrentVolume / ReagentUnit.New(Count + 1));
            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString($"There are { Count } slices remaining."));
        }
    }
}
