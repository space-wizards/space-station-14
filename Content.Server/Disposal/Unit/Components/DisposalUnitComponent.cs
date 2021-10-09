using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.DragDrop;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Disposal.Unit.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public class DisposalUnitComponent : SharedDisposalUnitComponent, IGasMixtureHolder, IDestroyAct
    {
        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit.
        /// </summary>
        [ViewVariables]
        public TimeSpan LastExitAttempt;

        /// <summary>
        ///     The current pressure of this disposal unit.
        ///     Prevents it from flushing if it is not equal to or bigger than 1.
        /// </summary>
        [ViewVariables]
        [DataField("pressure")]
        public float Pressure = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoEngageTime")]
        public readonly TimeSpan _automaticEngageTime = TimeSpan.FromSeconds(30);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flushDelay")]
        public readonly TimeSpan FlushDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        ///     Delay from trying to enter disposals ourselves.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("entryDelay")]
        private float _entryDelay = 0.5f;

        /// <summary>
        ///     Delay from trying to shove someone else into disposals.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _draggedEntryDelay = 0.5f;

        /// <summary>
        ///     Token used to cancel the automatic engage of a disposal unit
        ///     after an entity enters it.
        /// </summary>
        public CancellationTokenSource? AutomaticEngageToken;

        /// <summary>
        ///     Container of entities inside this disposal unit.
        /// </summary>
        [ViewVariables] public Container Container = default!;

        [ViewVariables] public IReadOnlyList<IEntity> ContainedEntities => Container.ContainedEntities;

        [ViewVariables]
        public bool Powered =>
            !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) ||
            receiver.Powered;

        [ViewVariables] public PressureState State => Pressure >= 1 ? PressureState.Ready : PressureState.Pressurizing;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Engaged { get; set; }

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(DisposalUnitUiKey.Key);

        [DataField("air")]
        public GasMixture Air { get; set; } = new GasMixture(Atmospherics.CellVolume);

        public async Task<bool> TryInsert(IEntity entity, IEntity? user = default)
        {
            if (!EntitySystem.Get<DisposalUnitSystem>().CanInsert(this, entity))
                return false;

            var delay = user == entity ? _entryDelay : _draggedEntryDelay;

            if (user != null && delay > 0.0f)
            {
                var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                // Can't check if our target AND disposals moves currently so we'll just check target.
                // if you really want to check if disposals moves then add a predicate.
                var doAfterArgs = new DoAfterEventArgs(user, delay, default, entity)
                {
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    NeedHand = false,
                };

                var result = await doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result == DoAfterStatus.Cancelled)
                    return false;
            }

            if (!Container.Insert(entity))
                return false;

            EntitySystem.Get<DisposalUnitSystem>().AfterInsert(this, entity);

            return true;
        }

        private bool PlayerCanUse(IEntity? player)
        {
            if (player == null)
            {
                return false;
            }

            var actionBlocker = EntitySystem.Get<ActionBlockerSystem>();

            if (!actionBlocker.CanInteract(player) ||
                !actionBlocker.CanUse(player))
            {
                return false;
            }

            return true;
        }

        public void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            if (!PlayerCanUse(obj.Session.AttachedEntity))
            {
                return;
            }

            if (obj.Message is not UiButtonPressedMessage message)
            {
                return;
            }

            switch (message.Button)
            {
                case UiButton.Eject:
                    EntitySystem.Get<DisposalUnitSystem>().TryEjectContents(this);
                    break;
                case UiButton.Engage:
                    EntitySystem.Get<DisposalUnitSystem>().ToggleEngage(this);
                    break;
                case UiButton.Power:
                    EntitySystem.Get<DisposalUnitSystem>().TogglePower(this);
                    SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Machines/machine_switch.ogg", Owner, AudioParams.Default.WithVolume(-2f));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool CanDragDropOn(DragDropEvent eventArgs)
        {
            // Base is redundant given this already calls the base CanInsert
            // If that changes then update this
            return EntitySystem.Get<DisposalUnitSystem>().CanInsert(this, eventArgs.Dragged);
        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            _ = TryInsert(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            EntitySystem.Get<DisposalUnitSystem>().TryEjectContents(this);
        }
    }
}
