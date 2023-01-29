using System.Linq;
using Content.Server.Light.Components;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class LightReplacerSystem : EntitySystem
    {
        [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LightReplacerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LightReplacerComponent, InteractUsingEvent>(HandleInteract);
            SubscribeLocalEvent<LightReplacerComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        private void OnInit(EntityUid uid, LightReplacerComponent replacer, ComponentInit args)
        {
            replacer.InsertedBulbs = ContainerHelpers.EnsureContainer<Container>(replacer.Owner, "light_replacer_storage");
        }

        private void HandleAfterInteract(EntityUid uid, LightReplacerComponent component, AfterInteractEvent eventArgs)
        {
            if (eventArgs.Handled)
                return;

            // standard interaction checks
            if (!eventArgs.CanReach) return;

            // behaviour will depends on target type
            if (eventArgs.Target != null)
            {
                var targetUid = (EntityUid) eventArgs.Target;

                // replace broken light in fixture?
                if (EntityManager.TryGetComponent(targetUid, out PoweredLightComponent? fixture))
                    eventArgs.Handled = TryReplaceBulb(uid, targetUid, eventArgs.User, component, fixture);
                // add new bulb to light replacer container?
                else if (EntityManager.TryGetComponent(targetUid, out LightBulbComponent? bulb))
                    eventArgs.Handled = TryInsertBulb(uid, targetUid, eventArgs.User, true, component, bulb);
            }
        }

        private void HandleInteract(EntityUid uid, LightReplacerComponent component, InteractUsingEvent eventArgs)
        {
            if (eventArgs.Handled)
                return;

            var usedUid = eventArgs.Used;

            // want to insert a new light bulb?
            if (EntityManager.TryGetComponent(usedUid, out LightBulbComponent? bulb))
                eventArgs.Handled = TryInsertBulb(uid, usedUid, eventArgs.User, true, component, bulb);
            // add bulbs from storage?
            else if (EntityManager.TryGetComponent(usedUid, out ServerStorageComponent? storage))
                eventArgs.Handled = TryInsertBulbsFromStorage(uid, usedUid, eventArgs.User, component, storage);
        }

        /// <summary>
        ///     Try to replace a light bulb in <paramref name="fixtureUid"/>
        ///     using light replacer. Light fixture should have <see cref="PoweredLightComponent"/>.
        /// </summary>
        /// <returns>True if successfully replaced light, false otherwise</returns>
        public bool TryReplaceBulb(EntityUid replacerUid, EntityUid fixtureUid, EntityUid? userUid = null,
            LightReplacerComponent? replacer = null, PoweredLightComponent? fixture = null)
        {
            if (!Resolve(replacerUid, ref replacer))
                return false;
            if (!Resolve(fixtureUid, ref fixture))
                return false;

            // check if light bulb is broken or missing
            var fixtureBulbUid = _poweredLight.GetBulb(fixture.Owner, fixture);
            if (fixtureBulbUid != null)
            {
                if (!EntityManager.TryGetComponent(fixtureBulbUid.Value, out LightBulbComponent? fixtureBulb))
                    return false;
                if (fixtureBulb.State == LightBulbState.Normal)
                    return false;
            }

            // try get first inserted bulb of the same type as targeted light fixtutre
            var bulb = replacer.InsertedBulbs.ContainedEntities.FirstOrDefault(
                (e) => EntityManager.GetComponentOrNull<LightBulbComponent>(e)?.Type == fixture.BulbType);

            // found bulb in inserted storage
            if (bulb.Valid) // FirstOrDefault can return default/invalid uid.
            {
                // try to remove it
                var hasRemoved = replacer.InsertedBulbs.Remove(bulb);
                if (!hasRemoved)
                    return false;
            }
            // try to create new instance of bulb from LightReplacerEntity
            else
            {
                var bulbEnt = replacer.Contents.FirstOrDefault((e) => e.Type == fixture.BulbType && e.Amount > 0);

                // found right bulb, let's spawn it
                if (bulbEnt != null)
                {
                    bulb = EntityManager.SpawnEntity(bulbEnt.PrototypeName, EntityManager.GetComponent<TransformComponent>(replacer.Owner).Coordinates);
                    bulbEnt.Amount--;
                }
                // not found any light bulbs
                else
                {
                    if (userUid != null)
                    {
                        var msg = Loc.GetString("comp-light-replacer-missing-light",
                            ("light-replacer", replacer.Owner));
                        _popupSystem.PopupEntity(msg, replacerUid, userUid.Value);
                    }
                    return false;
                }
            }

            // insert it into fixture
            var wasReplaced = _poweredLight.ReplaceBulb(fixtureUid, bulb, fixture);
            if (wasReplaced)
            {
                SoundSystem.Play(replacer.Sound.GetSound(),
                    Filter.Pvs(replacerUid), replacerUid, AudioParams.Default.WithVolume(-4f));
            }


            return wasReplaced;
        }

        /// <summary>
        ///     Try to insert a new bulb inside light replacer
        /// </summary>
        /// <returns>True if successfully inserted light, false otherwise</returns>
        public bool TryInsertBulb(EntityUid replacerUid, EntityUid bulbUid, EntityUid? userUid = null, bool showTooltip = false,
            LightReplacerComponent? replacer = null, LightBulbComponent? bulb = null)
        {
            if (!Resolve(replacerUid, ref replacer))
                return false;
            if (!Resolve(bulbUid, ref bulb))
                return false;

            // only normal (non-broken) bulbs can be inserted inside light replacer
            if (bulb.State != LightBulbState.Normal)
            {
                if (showTooltip && userUid != null)
                {
                    var msg = Loc.GetString("comp-light-replacer-insert-broken-light");
                    _popupSystem.PopupEntity(msg, replacerUid, userUid.Value);
                }

                return false;
            }

            // try insert light and show message
            var hasInsert = replacer.InsertedBulbs.Insert(bulb.Owner);
            if (hasInsert && showTooltip && userUid != null)
            {
                var msg = Loc.GetString("comp-light-replacer-insert-light",
                    ("light-replacer", replacer.Owner), ("bulb", bulb.Owner));
                _popupSystem.PopupEntity(msg, replacerUid, userUid.Value, PopupType.Medium);
            }

            return hasInsert;
        }

        /// <summary>
        ///     Try to insert all light bulbs from storage (for example light tubes box)
        /// </summary>
        /// <returns>
        ///     Returns true if storage contained at least one light bulb
        ///     which was successfully inserted inside light replacer
        /// </returns>
        public bool TryInsertBulbsFromStorage(EntityUid replacerUid, EntityUid storageUid, EntityUid? userUid = null,
            LightReplacerComponent? replacer = null, ServerStorageComponent? storage = null)
        {
            if (!Resolve(replacerUid, ref replacer))
                return false;
            if (!Resolve(storageUid, ref storage))
                return false;

            if (storage.StoredEntities == null)
                return false;

            var insertedBulbs = 0;
            var storagedEnts = storage.StoredEntities.ToArray();
            foreach (var ent in storagedEnts)
            {
                if (EntityManager.TryGetComponent(ent, out LightBulbComponent? bulb))
                {
                    if (TryInsertBulb(replacerUid, ent, userUid, false, replacer, bulb))
                        insertedBulbs++;
                }
            }

            // show some message if success
            if (insertedBulbs > 0 && userUid != null)
            {
                var msg = Loc.GetString("comp-light-replacer-refill-from-storage", ("light-replacer", storage.Owner));
                _popupSystem.PopupEntity(msg, replacerUid, userUid.Value, PopupType.Medium);
            }

            return insertedBulbs > 0;
        }
    }
}
