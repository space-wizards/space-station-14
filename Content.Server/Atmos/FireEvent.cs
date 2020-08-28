using Robust.Shared.GameObjects;

namespace Content.Server.Atmos
{
    public class FireActEvent : EntitySystemMessage
    {
        public float Temperature { get; }
        public float Volume { get; }

        public FireActEvent(float temperature, float volume)
        {
            Temperature = temperature;
            Volume = volume;
        }
    }
}
