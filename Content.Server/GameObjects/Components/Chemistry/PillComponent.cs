using Content.Server.GameObjects.Components.Body.Digestive;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Server.GameObjects.Components.Utensil;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    [ComponentReference(typeof(IAfterInteract))]
    public class PillComponent : FoodComponent, IUse, IAfterInteract
    {
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

        public override string Name => "Pill";

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _trashPrototype;
        [ViewVariables]
        private SolutionComponent _contents;
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
            _contents = Owner.GetComponent<SolutionComponent>();
            _transferAmount = _contents.CurrentVolume;

        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUseFood(eventArgs.User, null);
        }

        // Feeding someone else
        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
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

            if (!trueTarget.TryGetComponent(out StomachComponent stomach))
            {
                return false;
            }

            if (!user.InRangeUnobstructed(trueTarget, popup: true))
            {
                return false;
            }

            var transferAmount = ReagentUnit.Min(_transferAmount, _contents.CurrentVolume);
            var split = _contents.SplitSolution(transferAmount);
            if (!stomach.TryTransferSolution(split))
            {
                _contents.TryAddSolution(split);
                trueTarget.PopupMessage(user, Loc.GetString("You can't eat any more!"));
                return false;
            }

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
