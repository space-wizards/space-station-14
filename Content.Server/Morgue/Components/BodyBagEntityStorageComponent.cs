using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Paper;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
#pragma warning disable 618
    public class BodyBagEntityStorageComponent : EntityStorageComponent, IExamine, IInteractUsing
#pragma warning restore 618
    {
        public override string Name => "BodyBagEntityStorage";

        [ViewVariables]
        [ComponentDependency] private readonly AppearanceComponent? _appearance = null;

        [ViewVariables] public ContainerSlot? LabelContainer { get; private set; }

        protected override void Initialize()
        {
            base.Initialize();
            _appearance?.SetData(BodyBagVisuals.Label, false);
            LabelContainer = Owner.EnsureContainer<ContainerSlot>("body_bag_label", out _);
        }

        protected override bool AddToContents(IEntity entity)
        {
            if (entity.HasComponent<SharedBodyComponent>() && !EntitySystem.Get<StandingStateSystem>().IsDown(entity.Uid)) return false;
            return base.AddToContents(entity);
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                if (LabelContainer?.ContainedEntity != null && LabelContainer.ContainedEntity.TryGetComponent<PaperComponent>(out var paper))
                {
                    message.AddText(Loc.GetString("body-bag-entity-storage-component-on-examine-details", ("paper", paper.Content)));
                }
            }
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (LabelContainer == null) return false;

            if (LabelContainer.ContainedEntity != null)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("body-bag-entity-storage-component-interact-using-already-attached"));
                return false;
            }

            var handsComponent = eventArgs.User.GetComponent<IHandsComponent>();
            if (!handsComponent.Drop(eventArgs.Using, LabelContainer))
            {
                return false;
            }

            _appearance?.SetData(BodyBagVisuals.Label, true);

            Owner.PopupMessage(eventArgs.User, Loc.GetString("body-bag-entity-storage-component-interact-using-success",("entity", eventArgs.Using)));
            return true;
        }

        public void RemoveLabel(IEntity user)
        {
            if (LabelContainer == null) return;

            var ent = LabelContainer.ContainedEntity;
            if(ent is null)
                return;

            if (user.TryGetComponent(out HandsComponent? hands))
            {
                hands.PutInHandOrDrop(ent.GetComponent<ItemComponent>());
                _appearance?.SetData(BodyBagVisuals.Label, false);
            }
            else if (LabelContainer.Remove(ent))
            {
                ent.Transform.Coordinates = Owner.Transform.Coordinates;
                _appearance?.SetData(BodyBagVisuals.Label, false);
            }
        }
    }
}
