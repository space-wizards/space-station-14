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
using Robust.Shared.Interfaces.Network;
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
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public Ability Ability;

        public override void Initialize()
        {
            base.Initialize();

            Ability = new Ability("/Textures/Objects/Guns/Laser/laser_retro.rsi/laser_retro.png", TriggerAbility, new TimeSpan(10));
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case GetAbilitiesMessage msg:
                {
                    msg.Hotbar.AddAbility(Ability);
                    break;
                }
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case FireLaserCooldownMessage msg:
                {
                    Ability.Start = msg.Start;
                    Ability.End = msg.End;
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

            if (_gameTiming.CurTime < Ability.End) // + TimeSpan(latency) for prediction maybe?
            {
                return;
            }

            SendNetworkMessage(new FireLaserMessage(coords));
            return;
        }
    }
}
