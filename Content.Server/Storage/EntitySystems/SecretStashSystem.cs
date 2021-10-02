using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Storage.Components;
using Content.Server.Tools.Components;
using Content.Shared.Acts;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tool;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Storage.EntitySystems
{
    public class SecretStashTryHideItemEvent : EntityEventArgs
    {
        public SecretStashTryHideItemEvent(IEntity user, IEntity target, IEntity itemToHide)
        {
            User = user;
            Target = target;
            ItemToHide = itemToHide;
        }

        /// <summary>
        /// The entity which is trying to hide the item.
        /// </summary>
        public IEntity User { get; } = default!;

        /// <summary>
        /// The secret stash entity.
        /// </summary>
        public IEntity Target { get; } = default!;

        /// <summary>
        /// The entity which is trying to be hid.
        /// </summary>
        public IEntity ItemToHide { get; } = default!;
    }

    public class SecretStashTryGetItemEvent : EntityEventArgs
    {
        public SecretStashTryGetItemEvent(IEntity user, IEntity target)
        {
            User = user;
            Target = target;
        }

        /// <summary>
        /// The entity which is trying to get the item.
        /// </summary>
        public IEntity User { get; } = default!;

        /// <summary>
        /// The secret stash entity.
        /// </summary>
        public IEntity Target { get; } = default!;
    }

    [UsedImplicitly]
    public class SecretStashSystem : EntitySystem
    {
        private readonly List<IPlayerSession> _sessionCache = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SecretStashECSComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SecretStashECSComponent, SecretStashTryHideItemEvent>(OnTryHideItem);
            SubscribeLocalEvent<SecretStashECSComponent, SecretStashTryGetItemEvent>(OnTryGetItem);
            SubscribeLocalEvent<SecretStashECSComponent, DestructionEventArgs>(OnDestroyed);
        }

        private void OnInit(EntityUid eUI, SecretStashECSComponent comp, ComponentInit args)
        {
            comp.ItemContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(comp.Owner, "stash", out _);
        }

        private void OnTryHideItem(EntityUid eUI, SecretStashECSComponent comp, SecretStashTryHideItemEvent args)
        {
            PlayInteractSound(comp);
            if (comp.ItemContainer.ContainedEntity != null)
            {
                comp.Owner.PopupMessage(args.User, Loc.GetString("comp-secret-stash-action-hide-container-not-empty"));
                return;
            }

            if (!args.ItemToHide.TryGetComponent(out ItemComponent? item))
            {
                return;
            }

            if (item.Size > comp.MaxItemSize)
            {
                comp.Owner.PopupMessage(args.User,
                    Loc.GetString("comp-secret-stash-action-hide-item-too-big", ("item", args.ItemToHide), ("stash", comp.SecretPartName)));
                return;
            }

            if (!args.User.TryGetComponent(out IHandsComponent? hands))
            {
                return;
            }

            if (!hands.Drop(args.ItemToHide, comp.ItemContainer))
            {
                return;
            }

            comp.Owner.PopupMessage(args.User, Loc.GetString("comp-secret-stash-action-hide-success", ("item", args.ItemToHide), ("this", comp.SecretPartName)));
        }

        private void OnTryGetItem(EntityUid eUI, SecretStashECSComponent comp, SecretStashTryGetItemEvent args)
        {
            PlayInteractSound(comp);
            if (comp.ItemContainer.ContainedEntity == null)
            {
                return;
            }

            comp.Owner.PopupMessage(args.User, Loc.GetString("comp-secret-stash-action-get-item-found-something", ("stash", comp.SecretPartName)));

            if (args.User.TryGetComponent(out HandsComponent? hands))
            {
                if (!comp.ItemContainer.ContainedEntity.TryGetComponent(out ItemComponent? item))
                {
                    return;
                }
                hands.PutInHandOrDrop(item);
            }
            else if (comp.ItemContainer.Remove(comp.ItemContainer.ContainedEntity))
            {
                comp.ItemContainer.ContainedEntity.Transform.Coordinates = comp.Owner.Transform.Coordinates;
            }
        }

        // <summary>
        // Is there an item inside the secret stash item container?
        // </summary>
        public bool HasItemInside(SecretStashECSComponent comp)
        {
            PlayInteractSound(comp);
            return comp.ItemContainer.ContainedEntity != null;
        }

        /// <summary>
        /// Drops the stashed item
        /// </summary>
        public void OnDestroyed(EntityUid eUI, SecretStashECSComponent comp, DestructionEventArgs args)
        {
            if (comp.ItemContainer.ContainedEntity != null)
            {
                comp.ItemContainer.ContainedEntity.Transform.Coordinates = comp.Owner.Transform.Coordinates;
            }
        }

        private void PlayInteractSound(SecretStashECSComponent comp)
        {
            if(comp.InteractionSound != null)
            {
                SoundSystem.Play(Filter.Pvs(comp.Owner), comp.InteractionSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
            }           
        }
    }
}
