using System;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class PoweredLightComponent : Component
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "PoweredLight";

        [ComponentDependency]
        private readonly AppearanceComponent? _appearance;

        public TimeSpan LastThunk;
        public TimeSpan? LastGhostBlink;

        [DataField("burnHandSound")]
        public SoundSpecifier BurnHandSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

        [DataField("turnOnSound")]
        public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Machines/light_tube_on.ogg");

        [DataField("hasLampOnSpawn")]
        public bool HasLampOnSpawn = true;

        [ViewVariables]
        [DataField("on")]
        public bool On = true;

        [ViewVariables]
        public bool CurrentLit;

        [ViewVariables]
        public bool IsBlinking;

        [ViewVariables]
        [DataField("ignoreGhostsBoo")]
        public bool IgnoreGhostsBoo;

        [ViewVariables]
        [DataField("ghostBlinkingTime")]
        public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

        [ViewVariables]
        [DataField("ghostBlinkingCooldown")]
        public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);


        [DataField("bulb")] private LightBulbType _bulbType = LightBulbType.Tube;
        public LightBulbType BulbType;

        [ViewVariables] public ContainerSlot LightBulbContainer = default!;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [ViewVariables]
        public LightBulbComponent? LightBulb
        {
            get
            {
                if (LightBulbContainer.ContainedEntity == null) return null;

                LightBulbContainer.ContainedEntity.TryGetComponent(out LightBulbComponent? bulb);

                return bulb;
            }
        }

        /// <summary>
        /// Try to replace current bulb with a new one
        /// </summary>
        public bool ReplaceBulb(IEntity bulb)
        {
            //EjectBulb();
            //return InsertBulb(bulb);
            return false;
        }

    }
}
