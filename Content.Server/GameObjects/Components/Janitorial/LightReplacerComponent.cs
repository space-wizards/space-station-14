#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Janitorial
{
    /// <summary>
    ///     Device that allows user to quikly change bulbs in <see cref="PoweredLightComponent"/>
    ///     Can be reloaded by new light tubes or light bulbs
    /// </summary>
    [RegisterComponent]
    public class LightReplacerComponent : Component, IAfterInteract, IInteractUsing
    {
        public override string Name => "LightReplacer";

        [DataField("contents")] private List<LightReplacerEntity> _contents = new();

        [ViewVariables] private IContainer _bulbsStorage = default!;

        public override void Initialize()
        {
            base.Initialize();

            // fill light replacer container with bulbs
            _bulbsStorage = ContainerHelpers.EnsureContainer<Container>(Owner, "light_replacer_storage");
            foreach (var ent in _contents)
            {
                for (var i = 0; i < ent.Amount; i++)
                {
                    var bulb = Owner.EntityManager.SpawnEntity(ent.PrototypeName, Owner.Transform.Coordinates);
                    _bulbsStorage.Insert(bulb);
                }
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanUse(eventArgs.User)) return false;
            if (!eventArgs.CanReach) return false;

            // behaviour will depends on target type
            if (eventArgs.Target != null)
            {
                // replace broken light in fixture?
                if (eventArgs.Target.TryGetComponent(out PoweredLightComponent? fixture))
                    return TryReplaceBulb(fixture, eventArgs.User);
                // add new bulb to light replacer container?
                else if (eventArgs.Target.TryGetComponent(out LightBulbComponent? bulb))
                    return TryInsertBulb(bulb, eventArgs.User, true);
                // add bulbs from storage?
                else if (eventArgs.Target.TryGetComponent(out ServerStorageComponent? storage))
                    return TryInsertBulb(storage, eventArgs.User);
            }

            return false;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // standard interaction checks
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return false;

            if (eventArgs.Using != null)
            {
                // want to insert a new light bulb?
                if (eventArgs.Using.TryGetComponent(out LightBulbComponent? bulb))
                    return TryInsertBulb(bulb, eventArgs.User, true);
            }

            return false;
        }

        private bool TryReplaceBulb(PoweredLightComponent fixture, IEntity? user = null)
        {
            // check if light bulb is broken or missing
            if (fixture.LightBulb != null && fixture.LightBulb.State == LightBulbState.Normal) return false;

            // try get first bulb of the same type as targeted light fixtutre
            var bulb = _bulbsStorage.ContainedEntities.FirstOrDefault(
                (e) => e.GetComponentOrNull<LightBulbComponent>()?.Type == fixture.BulbType);

            // try to remove it from storage
            if (bulb == null || !_bulbsStorage.Remove(bulb))
            {
                if (user != null)
                {
                    var msg = Loc.GetString("comp-light-replacer-missing-light", ("light-replacer", Owner));
                    user.PopupMessage(msg);
                }
                return false;
            }


            // insert it into fixture
            return fixture.ReplacBulb(bulb);
        }

        private bool TryInsertBulb(LightBulbComponent bulb, IEntity? user = null, bool showTooltip = false)
        {
            // only normal lights can be inserted inside light replacer
            if (bulb.State != LightBulbState.Normal)
            {
                if (showTooltip && user != null)
                {
                    var msg = Loc.GetString("comp-light-replacer-insert-broken-light");
                    user.PopupMessage(msg);
                }

                return false;
            }

            return _bulbsStorage.Insert(bulb.Owner);
        }

        private bool TryInsertBulb(ServerStorageComponent storage, IEntity? user = null)
        {
            if (storage.StoredEntities == null)
                return false;

            var insertedBulbs = 0;
            var storagedEnts = storage.StoredEntities.ToArray();
            foreach (var ent in storagedEnts)
            {
                if (ent.TryGetComponent(out LightBulbComponent? bulb))
                {
                    if (TryInsertBulb(bulb))
                        insertedBulbs++;
                }
            }

            // show some message if success
            if (insertedBulbs > 0 && user != null)
            {
                var msg = Loc.GetString("comp-light-replacer-refill-from-storage", ("light-replacer", Owner));
                user.PopupMessage(msg);
            }

            return insertedBulbs > 0;
        }

        [Serializable]
        [DataDefinition]
        public struct LightReplacerEntity
        {
            [DataField("name", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
            public string? PrototypeName;

            [DataField("amount")]
            public int Amount;
        }
    }
}
