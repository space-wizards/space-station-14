#nullable enable
using System;
using System.Threading;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Shared.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class HandcuffComponent : Component, IAfterInteract
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Handcuff";

        /// <summary>
        ///     The time it takes to apply a <see cref="CuffedComponent"/> to an entity.
        /// </summary>
        [ViewVariables] private float cuffTime;

        /// <summary>
        ///     The time it takes to remove a <see cref="CuffedComponent"/> from an entity.
        /// </summary>
        [ViewVariables] private float uncuffTime;

        /// <summary>
        ///     The time it takes for a cuffed entity to remove <see cref="CuffedComponent"/> from itself.
        /// </summary>
        [ViewVariables] private float breakoutTime;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly float interactRange;

        private GridCoordinates startPosition;

        //private readonly SharedNotifyManager _notifyManager;

        public HandcuffComponent()
        {
            interactRange = SharedInteractionSystem.InteractionRange / 2;
            _cancellationTokenSource = new CancellationTokenSource();
            //IoCManager.InjectDependencies(this);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref cuffTime, "cuffTime", 5.0f);
            serializer.DataField(ref breakoutTime, "breakoutTime", 60.0f);
            serializer.DataField(ref uncuffTime, "uncuffTime", 10.0f);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null || !ActionBlockerSystem.CanUse(eventArgs.User))
            {
                return;
            }

            //HandsComponent isn't accessible in Shared
            /*if (!eventArgs.Target.TryGetComponent<HandsComponent>(out var sharedHands))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User,
                    "The target has no hands!");
            }*/

            if (eventArgs.Target == eventArgs.User)
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User,
                    "Cuffing yourself is a bad idea.");
            }

            if (!EntitySystem.Get<SharedInteractionSystem>()
                .InRangeUnobstructed(eventArgs.User.Transform.MapPosition,
                    eventArgs.Target.Transform.MapPosition, interactRange, ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(eventArgs.User, eventArgs.User,
                    "You are too far away to use the cuffs.");
                return;
            }

            _notifyManager.PopupMessage(eventArgs.User, eventArgs.User,
                $"You start cuffing {eventArgs.Target.Name}.");
            _notifyManager.PopupMessage(eventArgs.User, eventArgs.Target,
                $"{eventArgs.User.Name} starts cuffing you!");

            startPosition = eventArgs.Target.Transform.GridPosition;
            // TODO: do_after() once it exists
            Timer.Spawn(TimeSpan.FromSeconds(breakoutTime), () => Cuff(eventArgs.User, eventArgs.Target), _cancellationTokenSource.Token);
        }

        private void Cuff(IEntity user, IEntity target)
        {
            if (!EntitySystem.Get<SharedInteractionSystem>()
                .InRangeUnobstructed(user.Transform.MapPosition,
                    Owner.Transform.MapPosition, interactRange, ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(user, user, "You are too far away to use the cuffs.");
                return;
            }

            if (Owner.Transform.GridPosition != startPosition)
            {
                _notifyManager.PopupMessage(user, user, "You failed to use the cuffs, stand still next time.");
                return;
            }

            if (!ActionBlockerSystem.CanInteract(user))
            {
                _notifyManager.PopupMessage(user, user, "You fail to use the cuffs.");
                return;
            }

            _notifyManager.PopupMessage(user, user, $"You successfully cuff {target.Name}.");
            _notifyManager.PopupMessage(target, target, $"You have been cuffed by {user.Name}.");

            var cuffs = target.AddComponent<CuffedComponent>();
            cuffs.breakoutTime = breakoutTime;
            cuffs.uncuffTime = uncuffTime;
            if (Owner.Prototype != null)
            {
                cuffs.HandcuffId = Owner.Prototype.ID;
            }

            Owner.Delete();
        }

    }
}
