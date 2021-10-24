using Content.Shared.Light;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Light.Events
{
    /// <summary>
    ///     Invokes when light bulb changed its state
    /// </summary>
    public class BulbStateChangedEvent : EntityEventArgs
    {
        public EntityUid BulbUid;
        public LightBulbState State;

        public BulbStateChangedEvent(EntityUid bulbUid, LightBulbState state)
        {
            BulbUid = bulbUid;
            State = state;
        }
    }

    /// <summary>
    ///     Invokes when light bulb changed its color
    /// </summary>
    public class BulbColorChangedEvent : EntityEventArgs
    {
        public EntityUid BulbUid;
        public Color Color;

        public BulbColorChangedEvent(EntityUid bulbUid, Color color)
        {
            BulbUid = bulbUid;
            Color = color;
        }
    }
}
