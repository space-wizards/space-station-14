using System;
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

    public class LightBulbComponent : Component
    {

        public event EventHandler<EventArgs> OnLightBulbStateChange;

        public override string Name => "LightBulb";

        public LightBulbType Type = LightBulbType.Tube;

        public LightBulbState State
        {
            get { return _state; }
            set
            {
                OnLightBulbStateChange?.Invoke(this, EventArgs.Empty);
                _state = value;
            }
        }

        private LightBulbState _state = LightBulbState.Normal;

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Type, "bulb", LightBulbType.Tube);
        }

    }
}
