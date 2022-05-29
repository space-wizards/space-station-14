namespace Content.Server.Atmos
{
    /// <summary>
    ///     Event raised directed to an entity when it is standing on a tile that's on fire.
    /// </summary>
    [ByRefEvent]
    public readonly struct TileFireEvent
    {
        public readonly float Temperature;
        public readonly float Volume;

        public TileFireEvent(float temperature, float volume)
        {
            Temperature = temperature;
            Volume = volume;
        }
    }
}
