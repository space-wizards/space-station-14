using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Gravity
{
    [RegisterComponent]
    public class GravityGeneratorComponent: Component, IAttackBy
    {

        private PowerDeviceComponent _powerDevice;

        private SpriteComponent _sprite;

        private bool Powered => true;

        public GravityGeneratorStatus Status
        {
            get
            {
                if (!Intact) return GravityGeneratorStatus.Broken;
                if (!Powered) return GravityGeneratorStatus.Unpowered;
                if (!SwitchedOn) return GravityGeneratorStatus.Off;
                return GravityGeneratorStatus.On;
            }
        }

        public bool SwitchedOn = true;

        public bool Intact = true;
        public override string Name => "GravityGenerator";

        public override void Initialize()
        {
            base.Initialize();

            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _sprite = Owner.GetComponent<SpriteComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref SwitchedOn, "switched_on", true);
            serializer.DataField(ref Intact, "intact", true);
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            // TODO: Open UI if powered to flip the generator on or off, screw open to repair if broken
            return false;
        }

        public void UpdateSprite()
        {

        }
    }

    public enum GravityGeneratorStatus
    {
        Broken,
        Unpowered,
        Off,
        On
    }
}
