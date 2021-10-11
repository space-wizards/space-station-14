using Robust.Shared.GameObjects;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     Event raised directed to an entity when it is standing on a tile that's on fire.
    /// </summary>
    public class TileFireEvent : EntityEventArgs
    {
        public float Temperature { get; }
        public float Volume { get; }

        public TileFireEvent(float temperature, float volume)
        {
            Temperature = temperature;
            Volume = volume;
        }
    }
}
