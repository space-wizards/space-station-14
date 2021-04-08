#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
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
    public class LightReplacerComponent : Component
    {
        public override string Name => "LightReplacer";
        public override uint? NetID => ContentNetIDs.LIGHT_REPLACER;

        [DataField("sound")] private string _sound = "/Audio/Weapons/click.ogg";

        // bulbs that were inside light replacer when it spawned
        [DataField("contents")] private List<LightReplacerEntity> _contents = new();
        // bulbs that were inserted inside light replacer
        [ViewVariables] private IContainer _insertedBulbs = default!;

        public override void Initialize()
        {
            base.Initialize();
            _insertedBulbs = ContainerHelpers.EnsureContainer<Container>(Owner, "light_replacer_storage");
        }

        public bool TryReplaceBulb(PoweredLightComponent fixture, IEntity? user = null)
        {
            // check if light bulb is broken or missing
            if (fixture.LightBulb != null && fixture.LightBulb.State == LightBulbState.Normal) return false;

            // try get first inserted bulb of the same type as targeted light fixtutre
            var bulb = _insertedBulbs.ContainedEntities.FirstOrDefault(
                (e) => e.GetComponentOrNull<LightBulbComponent>()?.Type == fixture.BulbType);

            // found bulb in inserted storage
            if (bulb != null)
            {
                // try to remove it
                var hasRemoved = _insertedBulbs.Remove(bulb);
                if (!hasRemoved)
                    return false;
            }
            // try to create new instance of bulb from LightReplacerEntity
            else
            {
                var bulbEnt = _contents.FirstOrDefault((e) => e.Type == fixture.BulbType && e.Amount > 0);

                // found right bulb, let's spawn it
                if (bulbEnt != null)
                {
                    bulb = Owner.EntityManager.SpawnEntity(bulbEnt.PrototypeName, Owner.Transform.Coordinates);
                    bulbEnt.Amount--;
                }
                // not found any light bulbs
                else
                {
                    if (user != null)
                    {
                        var msg = Loc.GetString("comp-light-replacer-missing-light", ("light-replacer", Owner));
                        user.PopupMessage(msg);
                    }
                    return false;
                }
            }

            // insert it into fixture
            var wasReplaced = fixture.ReplaceBulb(bulb);
            if (wasReplaced)
            {
                EntitySystem.Get<AudioSystem>().Play(Filter.Broadcast(), _sound,
                    Owner, AudioParams.Default.WithVolume(-4f));
            }


            return wasReplaced;
        }

        public bool TryInsertBulb(LightBulbComponent bulb, IEntity? user = null, bool showTooltip = false)
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

            // try insert light and show message
            var hasInsert = _insertedBulbs.Insert(bulb.Owner);
            if (hasInsert && showTooltip && user != null)
            {
                var msg = Loc.GetString("comp-light-replacer-insert-light",
                    ("light-replacer", Owner), ("bulb", bulb.Owner));
                user.PopupMessage(msg);
            }


            return hasInsert;
        }

        public bool TryInsertBulb(ServerStorageComponent storage, IEntity? user = null)
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
        public class LightReplacerEntity
        {
            [DataField("name", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
            public string? PrototypeName;

            [DataField("amount")]
            public int Amount;

            [DataField("type")]
            public LightBulbType Type;
        }
    }
}
