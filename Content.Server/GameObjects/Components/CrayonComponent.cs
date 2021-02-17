#nullable enable
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class CrayonComponent : SharedCrayonComponent, IAfterInteract, IUse, IDropped
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        //TODO: useSound
        [DataField("useSound")]
        private string? _useSound;
        [ViewVariables(VVAccess.ReadWrite)]
        public int Charges { get; set; }
        [DataField("capacity")]
        private int _capacity = 30;
        [ViewVariables(VVAccess.ReadWrite)]
        public int Capacity { get => _capacity; set => _capacity = value; }

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CrayonUiKey.Key);

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
            Charges = Capacity;

            // Get the first one from the catalog and set it as default
            var decals = _prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
            if (decals != null)
            {
                SelectedState = decals.Decals.First();
            }
            Dirty();
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            switch (serverMsg.Message)
            {
                case CrayonSelectMessage msg:
                    // Check if the selected state is valid
                    var crayonDecals = _prototypeManager.EnumeratePrototypes<CrayonDecalPrototype>().FirstOrDefault();
                    if (crayonDecals != null)
                    {
                        if (crayonDecals.Decals.Contains(msg.State))
                        {
                            SelectedState = msg.State;
                            Dirty();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new CrayonComponentState(_color, SelectedState, Charges, Capacity);
        }

        // Opens the selection window
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                UserInterface?.Toggle(actor.playerSession);
                if (UserInterface?.SessionHasOpen(actor.playerSession) == true)
                {
                    // Tell the user interface the selected stuff
                    UserInterface.SetState(
                        new CrayonBoundUserInterfaceState(SelectedState, _color));
                }
                return true;
            }
            return false;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: false, popup: true,
                collisionMask: Shared.Physics.CollisionGroup.MobImpassable))
            {
                return true;
            }

            if (Charges <= 0)
            {
                eventArgs.User.PopupMessage(Loc.GetString("Not enough left."));
                return true;
            }

            var entityManager = IoCManager.Resolve<IServerEntityManager>();

            var entity = entityManager.SpawnEntity("CrayonDecal", eventArgs.ClickLocation);
            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(CrayonVisuals.State, SelectedState);
                appearance.SetData(CrayonVisuals.Color, _color);
                appearance.SetData(CrayonVisuals.Rotation, eventArgs.User.Transform.LocalRotation);
            }

            if (!string.IsNullOrEmpty(_useSound))
            {
                EntitySystem.Get<AudioSystem>().PlayFromEntity(_useSound, Owner, AudioHelpers.WithVariation(0.125f));
            }

            // Decrease "Ammo"
            Charges--;
            Dirty();
            return true;
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (eventArgs.User.TryGetComponent(out IActorComponent? actor))
                UserInterface?.Close(actor.playerSession);
        }
    }
}
