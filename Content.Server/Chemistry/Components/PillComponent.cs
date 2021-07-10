using System.Linq;
using System.Threading.Tasks;
using Content.Server.Body.Behavior;
using Content.Server.Nutrition.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public class PillComponent : FoodComponent, IUse, IAfterInteract
    {
        public override string Name => "Pill";

        [ViewVariables]
        [DataField("useSound")]
        protected override SoundSpecifier UseSound { get; set; } = default!;

        [ViewVariables]
        [DataField("trash")]
        protected override string? TrashPrototype { get; set; } = default;

        [ViewVariables]
        [DataField("transferAmount")]
        protected override ReagentUnit TransferAmount { get; set; } = ReagentUnit.New(1000);

        [ViewVariables]
        private SolutionContainerComponent _contents = default!;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out _contents);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUseFood(eventArgs.User, null);
        }

        // Feeding someone else
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            TryUseFood(eventArgs.User, eventArgs.Target);
            return true;
        }

        public override bool TryUseFood(IEntity? user, IEntity? target, UtensilComponent? utensilUsed = null)
        {
            if (user == null)
            {
                return false;
            }

            var trueTarget = target ?? user;

            if (!trueTarget.TryGetComponent(out SharedBodyComponent? body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                return false;
            }

            if (!user.InRangeUnobstructed(trueTarget, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(TransferAmount, _contents.CurrentVolume);
            var split = _contents.SplitSolution(transferAmount);

            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(split));

            if (firstStomach == null)
            {
                _contents.TryAddSolution(split);
                trueTarget.PopupMessage(user, Loc.GetString("pill-component-cannot-eat-more-message"));
                return false;
            }

            // TODO: Account for partial transfer.

            split.DoEntityReaction(trueTarget, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(split);

            if (UseSound.TryGetSound(out var sound))
            {
                SoundSystem.Play(Filter.Pvs(trueTarget), sound, trueTarget, AudioParams.Default.WithVolume(-1f));
            }

            trueTarget.PopupMessage(user, Loc.GetString("pill-component-swallow-success-message"));

            Owner.QueueDelete();
            return true;
        }
    }
}
