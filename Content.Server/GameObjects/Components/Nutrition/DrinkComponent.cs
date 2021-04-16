#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body.Behavior;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class DrinkComponent : Component, IUse, IAfterInteract, ISolutionChange
    {
        public override string Name => "Drink";

        int IAfterInteract.Priority => 10;

        [ViewVariables]
        [DataField("useSound")]
        private string _useSound = "/Audio/Items/drink.ogg";

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit TransferAmount { get; [UsedImplicitly] private set; } = ReagentUnit.New(2);
        [ViewVariables]
        public bool Empty => Owner.GetComponentOrNull<ISolutionInteractionsComponent>()?.DrainAvailable <= 0;

        public override void Initialize()
        {
            base.Initialize();
            UpdateAppearance();
        }

        void ISolutionChange.SolutionChanged(SolutionChangeEventArgs eventArgs)
        {
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance) ||
                !Owner.TryGetComponent(out ISolutionInteractionsComponent? contents))
            {
                return;
            }

            appearance.SetData(SharedFoodComponent.FoodVisuals.Visual, contents.DrainAvailable.Float());
        }

        bool IUse.UseEntity(UseEntityEventArgs args)
        {
            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? contents) ||
                contents.DrainAvailable <= 0)
            {
                args.User.PopupMessage(Loc.GetString("{0:theName} is empty!", Owner));
                return true;
            }

            return TryUseDrink(args.User, args.User);
        }

        //Force feeding a drink to someone.
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (Owner.TryGetComponent(out SolutionContainerCapComponent? cap) && !cap.Opened)
            {
                eventArgs.Target.PopupMessage(Loc.GetString("Open {0:theName} first!", Owner));
                return false;
            }

            return TryUseDrink(eventArgs.User, eventArgs.Target, true);
        }

        private bool TryUseDrink(IEntity user, IEntity target, bool forced = false)
        {

            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? interactions) ||
                !interactions.CanDrain ||
                interactions.DrainAvailable <= 0)
            {
                if (!forced)
                {
                    target.PopupMessage(Loc.GetString("{0:theName} is empty!", Owner));
                }

                return false;
            }

            if (!target.TryGetComponent(out IBody? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                target.PopupMessage(Loc.GetString("You can't drink {0:theName}!", Owner));
                return false;
            }


            if (user != target &&
                !user.InRangeUnobstructed(target, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(TransferAmount, interactions.DrainAvailable);
            var drain = interactions.Drain(transferAmount);
            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(drain));

            // All stomach are full or can't handle whatever solution we have.
            if (firstStomach == null)
            {
                target.PopupMessage(Loc.GetString("You've had enough {0:theName}!", Owner));

                if (!interactions.CanRefill)
                {
                    drain.SpillAt(target, "PuddleSmear");
                    return false;
                }

                interactions.Refill(drain);
                return false;
            }

            if (!string.IsNullOrEmpty(_useSound))
            {
                SoundSystem.Play(Filter.Pvs(target), _useSound, target, AudioParams.Default.WithVolume(-2f));
            }

            target.PopupMessage(Loc.GetString("Slurp"));
            UpdateAppearance();

            // TODO: Account for partial transfer.

            drain.DoEntityReaction(target, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(drain);

            return true;
        }
    }
}
