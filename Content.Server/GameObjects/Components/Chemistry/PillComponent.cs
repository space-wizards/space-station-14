using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body.Behavior;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.Components.Culinary;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class PillComponent : FoodComponent, IUse, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Pill";

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _trashPrototype;
        [ViewVariables]
        private SolutionContainerComponent _contents;
        [ViewVariables]
        private ReagentUnit _transferAmount;


        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _useSound, "useSound", null);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(1000));
            serializer.DataField(ref _trashPrototype, "trash", null);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out _contents);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUseFood(eventArgs.User, null);
        }

        // Feeding someone else
        public async Task AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return;
            }

            TryUseFood(eventArgs.User, eventArgs.Target);
        }

        public override bool TryUseFood(IEntity user, IEntity target, UtensilComponent utensilUsed = null)
        {
            if (user == null)
            {
                return false;
            }

            var trueTarget = target ?? user;

            if (!trueTarget.TryGetComponent(out IBody body) ||
                !body.TryGetMechanismBehaviors<StomachBehavior>(out var stomachs))
            {
                return false;
            }

            if (!user.InRangeUnobstructed(trueTarget, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(_transferAmount, _contents.CurrentVolume);
            var split = _contents.SplitSolution(transferAmount);

            var firstStomach = stomachs.FirstOrDefault(stomach => stomach.CanTransferSolution(split));

            if (firstStomach == null)
            {
                _contents.TryAddSolution(split);
                trueTarget.PopupMessage(user, Loc.GetString("You can't eat any more!"));
                return false;
            }

            // TODO: Account for partial transfer.

            split.DoEntityReaction(trueTarget, ReactionMethod.Ingestion);

            firstStomach.TryTransferSolution(split);

            if (_useSound != null)
            {
                _entitySystem.GetEntitySystem<AudioSystem>()
                    .PlayFromEntity(_useSound, trueTarget, AudioParams.Default.WithVolume(-1f));
            }

            trueTarget.PopupMessage(user, Loc.GetString("You swallow the pill."));

            Owner.Delete();
            return true;
        }
    }
}
