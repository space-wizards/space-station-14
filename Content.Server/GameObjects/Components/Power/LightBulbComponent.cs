using System;
using SS14.Server.GameObjects;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Power
{
    public enum LightBulbState
    {
        Normal,
        Broken,
        Burned,
    }

    public enum LightBulbType
    {
        Tube,
    }

    /// <summary>
    ///     Component that represents a light bulb. Can be broken, or burned, which turns them mostly useless.
    /// </summary>
    public class LightBulbComponent : Component
    {

        /// <summary>
        ///     Invoked whenever the state of the light bulb changes.
        /// </summary>
        public event EventHandler<EventArgs> OnLightBulbStateChange;

        public override string Name => "LightBulb";

        public LightBulbType Type = LightBulbType.Tube;

        /// <summary>
        ///     The current state of the light bulb. Invokes the OnLightBulbStateChange event when set.
        ///     It also updates the bulb's sprite accordingly.
        /// </summary>
        public LightBulbState State
        {
            get { return _state; }
            set
            {
                var sprite = Owner.GetComponent<SpriteComponent>();
                OnLightBulbStateChange?.Invoke(this, EventArgs.Empty);
                _state = value;
                switch (value)
                {
                    case LightBulbState.Normal:
                        sprite.LayerSetState(0, "normal");
                        break;
                    case LightBulbState.Broken:
                        sprite.LayerSetState(0, "broken");
                        break;
                    case LightBulbState.Burned:
                        sprite.LayerSetState(0, "burned");
                        break;
                }
            }
        }

        private LightBulbState _state = LightBulbState.Normal;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Type, "bulb", LightBulbType.Tube);
        }

    }
}
