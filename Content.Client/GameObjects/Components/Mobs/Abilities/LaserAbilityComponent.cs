using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Mobs.Abilities;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Client.GameObjects.Components.Mobs.Abilities
{
    [RegisterComponent]
    public class LaserAbilityComponent : SharedLaserAbilityComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        private const float MaxLength = 20;

        public List<Ability> Abilities = new List<Ability>();

        string _spritename;
        private int _damage;
        private int _baseFireCost;
        private float _lowerChargeLimit;
        private string _fireSound;

        public override void Initialize()
        {
            base.Initialize();

            var ability = new Ability("Textures/Objects/Guns/Laser/laser_cannon.rsi/laser_cannon.png", TriggerAbility, new TimeSpan(10));

            Abilities.Add(ability);

            if (Owner.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                hotbarComponent.AddAbility(ability);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spritename, "fireSprite", "Objects/laser.png");
            serializer.DataField(ref _damage, "damage", 10);
            serializer.DataField(ref _baseFireCost, "baseFireCost", 300);
            serializer.DataField(ref _lowerChargeLimit, "lowerChargeLimit", 10);
            serializer.DataField(ref _fireSound, "fireSound", "/Audio/laser.ogg");
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case GetAbilitiesMessage msg:
                {
                    foreach (var ability in Abilities)
                    {
                        msg.Hotbar.AddAbility(ability);
                    }
                    break;
                }
            }
        }

        private void TriggerAbility(ICommonSession session, GridCoordinates coords, EntityUid uid, Ability ability)
        {
            if (!Owner.IsValid())
            {
                return;
            }
            if (_gameTiming.CurTime < ability.End)
            {
                return;
            }

            ability.Start = _gameTiming.CurTime;
            ability.End = ability.Start + ability.Cooldown;

            var player = session.AttachedEntity;
            SendNetworkMessage(new FireLaserMessage(player, coords));
            return;
        }
    }
}
