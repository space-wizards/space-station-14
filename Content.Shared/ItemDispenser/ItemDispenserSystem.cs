using Content.Shared.ActionBlocker;
using Robust.Shared.Audio;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.ItemDispenser
{
    /// <summary>
    /// Handles the interactions for a single item type dispenser
    /// </summary>
    public sealed class ItemDispenserSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemDispenserComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ItemDispenserComponent, ComponentInit>(OnInitialize);

            SubscribeLocalEvent<ItemDispenserComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ItemDispenserComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
            SubscribeLocalEvent<ItemDispenserComponent, ExaminedEvent>(OnExamined);

            SubscribeLocalEvent<ItemDispenserComponent, BreakageEventArgs>(OnBreak);
            SubscribeLocalEvent<ItemDispenserComponent, DestructionEventArgs>(OnBreak);
        }

        private void OnInitialize(EntityUid uid, ItemDispenserComponent dispenser, ComponentInit args)
        {
            dispenser.Storage = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
        }

        /// <summary>
        ///  Fill up container on init
        /// </summary>
        private void OnMapInit(EntityUid uid, ItemDispenserComponent dispenser, MapInitEvent args)
        {
            if (!dispenser.FillOnInit)
                return;

            for(int i = 0; i < dispenser.Capacity; i++ )
            {
                var itemEntity = EntityManager.SpawnEntity(dispenser.ItemId, _entManager.GetComponent<TransformComponent>(dispenser.Owner).Coordinates);
                dispenser.Storage?.Insert(itemEntity);
            }
        }

        /// <summary>
        ///     Adds an alt verb for restocking items
        /// </summary>
        private void OnGetAltVerbs(EntityUid uid, ItemDispenserComponent dispenser, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || args.Using == null)
                return;

            var itemUsed = (EntityUid) args.Using;

            if (!CanRestock(uid, dispenser, itemUsed))
                return;

            var itemName = _entManager.GetComponent<MetaDataComponent>(itemUsed).EntityName;

            AlternativeVerb verb = new();
            verb.IconEntity = args.Using;
            verb.Text = Loc.GetString("item-dispenser-component-restock-verb", ("itemName", itemName));
            verb.Category = VerbCategory.Insert;

            verb.Act = () => { Restock(uid, dispenser, itemUsed); };

            args.Verbs.Add(verb);
        }

        private bool CanRestock(EntityUid uid, ItemDispenserComponent dispenser, EntityUid itemUsed)
        {
            return dispenser.RestockWhitelist?.IsValid(itemUsed) == true
                && dispenser.Storage?.ContainedEntities.Count < dispenser.Capacity;
        }

        private void Restock(EntityUid uid, ItemDispenserComponent dispenser, EntityUid itemUsed)
        {
            dispenser.Storage?.Insert(itemUsed);
            PlaySound(uid, dispenser.RestockSound, dispenser.SoundOptions);
        }

        /// <summary>
        ///  Attempt to dispense an item into the used hand.
        /// </summary>
        private void OnInteractHand(EntityUid uid, ItemDispenserComponent dispenser, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            TryDispense(uid, dispenser, args.User);
        }

        private bool TryDispense(EntityUid uid, ItemDispenserComponent dispenser, EntityUid? _user)
        {
            if (_user == null || !_user.HasValue || dispenser.Storage == null)
                return false;

            if(dispenser.Storage.ContainedEntities.Count < 1)
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
        private void OnBreak(EntityUid uid, ItemDispenserComponent dispenser, EntityEventArgs args)
        {
            if (dispenser.Storage == null)
                return;

            int count = dispenser.Storage.ContainedEntities.Count;
            for (int i = 0; i < count; i++)
            {
                dispenser.Storage.Remove(dispenser.Storage.ContainedEntities.First());
            }
        }

        private void OnExamined(EntityUid uid, ItemDispenserComponent dispenser, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !dispenser.AllowStockExamine || dispenser.Storage == null)
                return;

            if (dispenser.Storage.ContainedEntities.Count > 0)
            {
                var itemName = _entManager.GetComponent<MetaDataComponent>(dispenser.Storage.ContainedEntities.First()).EntityName;

                args.PushMarkup(Loc.GetString("item-dispenser-component-remaining-stock",
                    ("amount", dispenser.Storage.ContainedEntities.Count),
                    ("capacity", dispenser.Capacity),
                    ("item", itemName)));
            }
            else
            {
                args.PushMarkup(Loc.GetString("item-dispenser-component-out-of-stock"));
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
