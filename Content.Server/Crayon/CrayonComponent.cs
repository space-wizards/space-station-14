using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Crayon;
using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Crayon
{
    [RegisterComponent]
    public class CrayonComponent : SharedCrayonComponent, IAfterInteract, IUse, IDropped, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [DataField("useSound")]
        private SoundSpecifier? _useSound = null;

        [ViewVariables]
        public Color Color { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Charges { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("capacity")]
        public int Capacity { get; set; } = 30;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CrayonUiKey.Key);

        void ISerializationHooks.AfterDeserialization()
        {
            Color = Color.FromName(_color);
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
            Charges = Capacity;

            // Get the first one from the catalog and set it as default
            var decal = _prototypeManager.EnumeratePrototypes<DecalPrototype>().FirstOrDefault(x => x.Tags.Contains("crayon"));
            SelectedState = decal?.ID ?? string.Empty;
            Dirty();
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            switch (serverMsg.Message)
            {
                case CrayonSelectMessage msg:
                    // Check if the selected state is valid
                    if (_prototypeManager.TryIndex<DecalPrototype>(msg.State, out var prototype) && prototype.Tags.Contains("crayon"))
                    {
                        SelectedState = msg.State;
                        Dirty();
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
            if (_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
            {
                UserInterface?.Toggle(actor.PlayerSession);
                if (UserInterface?.SessionHasOpen(actor.PlayerSession) == true)
                {
                    // Tell the user interface the selected stuff
                    UserInterface.SetState(
                        new CrayonBoundUserInterfaceState(SelectedState, Color));
                }
                return true;
            }
            return false;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.User.InRangeUnobstructed(eventArgs.ClickLocation, ignoreInsideBlocker: false, popup: true,
                collisionMask: Shared.Physics.CollisionGroup.MobImpassable))
            {
                return true;
            }

            if (Charges <= 0)
            {
                eventArgs.User.PopupMessage(Loc.GetString("crayon-interact-not-enough-left-text"));
                return true;
            }

            if (!eventArgs.ClickLocation.IsValid(_entMan))
            {
                eventArgs.User.PopupMessage(Loc.GetString("crayon-interact-invalid-location"));
                return true;
            }

            if(!EntitySystem.Get<DecalSystem>().TryAddDecal(SelectedState, eventArgs.ClickLocation.Offset(new Vector2(-0.5f,-0.5f)), out _, Color.FromName(_color), cleanable: true))
                return false;

            if (_useSound != null)
                SoundSystem.Play(Filter.Pvs(Owner), _useSound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f));

            // Decrease "Ammo"
            Charges--;
            Dirty();
            EntitySystem.Get<AdminLogSystem>().Add(LogType.CrayonDraw, LogImpact.Low, $"{_entMan.ToPrettyString(eventArgs.User):user} drew a {_color:color} {SelectedState}");
            return true;
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if (_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                UserInterface?.Close(actor.PlayerSession);
        }
    }
}
