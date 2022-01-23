using Content.Server.Light.EntitySystems;
using Content.Shared.Acts;
using Content.Shared.Light;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a light bulb. Can be broken, or burned, which turns them mostly useless.
    /// </summary>
    [RegisterComponent, Friend(typeof(LightBulbSystem))]
    public class LightBulbComponent : Component, IBreakAct
    {
        public override string Name => "LightBulb";

        [DataField("color")]
        public Color Color = Color.White;

        [DataField("bulb")]
        public LightBulbType Type = LightBulbType.Tube;

        [DataField("startingState")]
        public LightBulbState State = LightBulbState.Normal;

        [DataField("BurningTemperature")]
        public int BurningTemperature = 1400;

        [DataField("lightEnergy")]
        public float LightEnergy = 0.8f;

        [DataField("lightRadius")]
        public float LightRadius = 10;

        [DataField("lightSoftness")]
        public float LightSoftness = 1;

        [DataField("PowerUse")]
        public int PowerUse = 40;

        [DataField("breakSound")]
        public SoundSpecifier BreakSound = new SoundCollectionSpecifier("GlassBreak");

        // TODO: move me to ECS
        public void OnBreak(BreakageEventArgs eventArgs)
        {
            EntitySystem.Get<LightBulbSystem>()
                .SetState(Owner, LightBulbState.Broken, this);
        }
    }
}
