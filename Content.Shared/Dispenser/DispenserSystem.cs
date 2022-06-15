using Robust.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.ActionBlocker;

namespace Content.Shared.Dispenser
{
    /// <summary>
    /// Handles the interactions with a single item type dispenser
    /// </summary>
    public sealed class DispenserSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DispenserComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DispenserComponent, ComponentInit>(OnInitialize);

            SubscribeLocalEvent<DispenserComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<DispenserComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

            SubscribeLocalEvent<DispenserComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<DispenserComponent, DestructionEventArgs>(OnBreak);
        }

        private void OnInitialize(EntityUid uid, DispenserComponent component, ComponentInit args)
        {
            component.Storage = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
        }

        /// <summary>
        ///  Fill up container on init
        /// </summary>
        private void OnMapInit(EntityUid uid, DispenserComponent component, MapInitEvent args)
        {
            for(int i = 0; i < component.Capacity; i++ )
            {
                var itemEntity = EntityManager.SpawnEntity(component.ItemId, _entManager.GetComponent<TransformComponent>(component.Owner).Coordinates);
                component.Storage?.Insert(itemEntity);
            }
        }

        /// <summary>
        ///     Adds an alt verb for restocking items
        /// </summary>
        private void OnGetAltVerbs(EntityUid uid, DispenserComponent dispenser, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || args.Using == null)
                return;

            var itemUsed = (EntityUid) args.Using;

            AlternativeVerb verb = new();
            verb.IconEntity = args.Using;
            verb.Act = () =>
            {
                if (TryRestock(uid, dispenser, itemUsed))
                    PlaySound(uid, dispenser.RestockSound, dispenser.SoundOptions);
            };

            verb.Text = Loc.GetString("dispenser-component-restock-verb");
            verb.Category = VerbCategory.Insert;

            args.Verbs.Add(verb);
        }

        private bool TryRestock(EntityUid uid, DispenserComponent dispenser, EntityUid itemUsed)
        {
            //restocking one item
            if (dispenser.WhiteList?.IsValid(itemUsed) == true
                && dispenser.Storage?.ContainedEntities.Count < dispenser.Capacity)
            {
                return dispenser.Storage?.Insert(itemUsed) == true;
            }

            //TODO: handle item stacks

            return false;
        }

        /// <summary>
        ///  Attempt to dispense an item into the used hand.
        /// </summary>
        private void OnInteractHand(EntityUid uid, DispenserComponent dispenser, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            TryDispense(uid, dispenser, args.User);
            args.Handled = true;
        }

        private bool TryDispense(EntityUid uid, DispenserComponent dispenser, EntityUid? _user)
        {
            if (_user == null || !_user.HasValue || dispenser.Storage == null)
                return false;

            if(dispenser.Storage.ContainedEntities.Count <= 0)
                return false;

            EntityUid user = (EntityUid) _user;

            var item = dispenser.Storage.ContainedEntities.First();
            if(dispenser.Storage.CanRemove(item) && _actionBlockerSystem.CanPickup(user, item))
            {
                dispenser.Storage.Remove(item);
                _handsSystem.PickupOrDrop(user, item);
                PlaySound(uid, dispenser.DispenseSound, dispenser.SoundOptions);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Dumps the remaining items onto the ground upon breaking.
        /// </summary>
        private void OnBreak(EntityUid uid, DispenserComponent component, EntityEventArgs args)
        {
            if (component.Storage == null)
                return;

            int count = component.Storage.ContainedEntities.Count;
            for (int i = 0; i < count; i++)
            {
                component.Storage.Remove(component.Storage.ContainedEntities.First());
            }
        }

        /// <summary>
        /// Plays a sound for dispense/restock
        /// </summary>
        private void PlaySound(EntityUid uid, SoundSpecifier? sound, AudioParams audioParams)
        {
            if (sound == null || !_gameTiming.IsFirstTimePredicted)
                return;

            SoundSystem.Play(sound.GetSound(), Filter.Local(), uid, audioParams);
        }
    }
}
