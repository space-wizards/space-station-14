using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using Robust.Shared.Maths;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class PipeDispenserComponent : SharedPipeDispenserComponent, IActivate, IBreakAct
    {

        private bool _ejecting;
        private TimeSpan _ejectDelay = TimeSpan.FromSeconds(2);

        private List<string> _inventory;

        private bool Powered => !Owner.TryGetComponent(out PowerReceiverComponent receiver) || receiver.Powered;
        private bool _broken;

        private string _soundVend = "";

        private Direction _accessDirection;

        [ViewVariables] private BoundUserInterface UserInterface => Owner.GetUIOrNull(PipeDispenserUiKey.Key);

        public void Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
                return;
            if (!Powered)
                return;
            _accessDirection = DirectionTo(actor.Owner);
            UserInterface?.Open(actor.playerSession);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _inventory, "inventory", new List<string>());
            // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
            serializer.DataField(ref _soundVend, "soundVend", "/Audio/Machines/machine_vend.ogg");
            //serializer.DataField(ref _ejectDelay, "delay", TimeSpan.FromSeconds(2));
        }

        public override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            if (Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                receiver.OnPowerStateChanged += UpdatePower;
                TrySetVisualState(receiver.Powered ? PipeDispenserVisualState.Normal : PipeDispenserVisualState.Off);
            }

            var inventory = new List<PipeDispenserInventoryEntry>();
            foreach (var id in _inventory)
            {
                inventory.Add(new PipeDispenserInventoryEntry(id));
            }

            Inventory = inventory;
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                receiver.OnPowerStateChanged -= UpdatePower;
            }

            base.OnRemove();
        }

        private void UpdatePower(object sender, PowerStateEventArgs args)
        {
            var state = args.Powered ? PipeDispenserVisualState.Normal : PipeDispenserVisualState.Off;
            TrySetVisualState(state);
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (!Powered)
                return;

            var message = serverMsg.Message;
            switch (message)
            {
                case PipeDispenserEjectMessage msg:
                    TryEject(msg.ID, msg.Amount);
                    break;
                case InventorySyncRequestMessage _:
                    UserInterface?.SendMessage(new PipeDispenserInventoryMessage(Inventory));
                    break;
            }
        }

        private void TryEject(string id, uint amount)
        {
            if (_ejecting || _broken)
            {
                return;
            }

            var entry = Inventory.Find(x => x.ID == id);
            if (entry == null)
            {
                return;
            }

            _ejecting = true;
            TrySetVisualState(PipeDispenserVisualState.Eject);

            Timer.Spawn(_ejectDelay, () =>
            {
                _ejecting = false;
                TrySetVisualState(PipeDispenserVisualState.Normal);
                for (int i = 0; i < amount; i++)
                {
                    var e = Owner.EntityManager.SpawnEntity(id, Owner.Transform.GridPositio);
                    if(e.TryGetComponent(out CollidableComponent collidable))
                        collidable.Anchored = false;
                }
            });

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_soundVend, Owner, AudioParams.Default.WithVolume(-2f));
        }

        private void TrySetVisualState(PipeDispenserVisualState state)
        {
            var finalState = state;
            if (_broken)
            {
                finalState = PipeDispenserVisualState.Broken;
            }
            else if (_ejecting)
            {
                finalState = PipeDispenserVisualState.Eject;
            }
            else if (!Powered)
            {
                finalState = PipeDispenserVisualState.Off;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(PipeDispenserVisuals.VisualState, finalState);
            }
        }

        private Direction DirectionTo(IEntity other)
        {
            return (other.Transform.WorldPosition - Owner.Transform.WorldPosition).GetDir();
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _broken = true;
            TrySetVisualState(PipeDispenserVisualState.Broken);
        }
    }
}
