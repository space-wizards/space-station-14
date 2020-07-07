using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Gravity;
using Content.Shared.GameObjects.Components.Interactable;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Gravity
{
    [RegisterComponent]
    public class GravityGeneratorComponent: SharedGravityGeneratorComponent, IInteractUsing, IBreakAct, IInteractHand
    {
        private BoundUserInterface _userInterface;

        private PowerReceiverComponent _powerReceiver;

        private SpriteComponent _sprite;

        private bool _switchedOn;

        private bool _intact;

        private GravityGeneratorStatus _status;

        public bool Powered => _powerReceiver.Powered;

        public bool SwitchedOn => _switchedOn;

        public bool Intact => _intact;

        public GravityGeneratorStatus Status => _status;

        public bool NeedsUpdate
        {
            get
            {
                switch (_status)
                {
                    case GravityGeneratorStatus.On:
                        return !(Powered && SwitchedOn && Intact);
                    case GravityGeneratorStatus.Off:
                        return SwitchedOn || !(Powered && Intact);
                    case GravityGeneratorStatus.Unpowered:
                        return SwitchedOn || Powered || !Intact;
                    case GravityGeneratorStatus.Broken:
                        return SwitchedOn || Powered || Intact;
                    default:
                        return true; // This _should_ be unreachable
                }
            }
        }

        public override string Name => "GravityGenerator";

        public override void Initialize()
        {
            base.Initialize();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(GravityGeneratorUiKey.Key);
            _userInterface.OnReceiveMessage += HandleUIMessage;
            _powerReceiver = Owner.GetComponent<PowerReceiverComponent>();
            _sprite = Owner.GetComponent<SpriteComponent>();
            _switchedOn = true;
            _intact = true;
            _status = GravityGeneratorStatus.On;
            UpdateState();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _switchedOn, "switched_on", true);
            serializer.DataField(ref _intact, "intact", true);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<IActorComponent>(out var actor))
                return false;
            if (Status != GravityGeneratorStatus.Off && Status != GravityGeneratorStatus.On)
            {
                return false;
            }
            OpenUserInterface(actor.playerSession);
            return true;
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out WelderComponent tool))
                return false;

            if (!tool.UseTool(eventArgs.User, Owner, ToolQuality.Welding, 5f))
                return false;

            // Repair generator
            var damageable = Owner.GetComponent<DamageableComponent>();
            var breakable = Owner.GetComponent<BreakableComponent>();
            damageable.HealAllDamage();
            breakable.broken = false;
            _intact = true;

            var notifyManager = IoCManager.Resolve<IServerNotifyManager>();

            notifyManager.PopupMessage(Owner, eventArgs.User, Loc.GetString("You repair the gravity generator with the welder"));

            return true;
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            _intact = false;
            _switchedOn = false;
        }

        public void UpdateState()
        {
            if (!Intact)
            {
                MakeBroken();
            } else if (!Powered)
            {
                MakeUnpowered();
            } else if (!SwitchedOn)
            {
                MakeOff();
            } else
            {
                MakeOn();
            }
        }

        private void HandleUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case GeneratorStatusRequestMessage _:
                    _userInterface.SetState(new GeneratorState(Status == GravityGeneratorStatus.On));
                    break;
                case SwitchGeneratorMessage msg:
                    _switchedOn = msg.On;
                    UpdateState();
                    break;
                default:
                    break;
            }
        }

        private void OpenUserInterface(IPlayerSession playerSession)
        {
            _userInterface.Open(playerSession);
        }

        private void MakeBroken()
        {
            _status = GravityGeneratorStatus.Broken;
            _sprite.LayerSetState(0, "broken");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeUnpowered()
        {
            _status = GravityGeneratorStatus.Unpowered;
            _sprite.LayerSetState(0, "off");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeOff()
        {
            _status = GravityGeneratorStatus.Off;
            _sprite.LayerSetState(0, "off");
            _sprite.LayerSetVisible(1, false);
        }

        private void MakeOn()
        {
            _status = GravityGeneratorStatus.On;
            _sprite.LayerSetState(0, "on");
            _sprite.LayerSetVisible(1, true);
        }
    }

    public enum GravityGeneratorStatus
    {
        Broken,
        Unpowered,
        Off,
        On
    }
}
