using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public class TemperatureExposeEvent : EntitySystemMessage
    {
        public MapIndices Indices { get; }
        public GridId Grid { get; }
        public GasMixture Air { get; }
        public float Temperature { get; }
        public float Volume { get; }

        public TemperatureExposeEvent(MapIndices indices, GridId gridId, GasMixture air, float temperature, float volume)
        {
            Indices = indices;
            Grid = gridId;
            Air = air;
            Temperature = temperature;
            Volume = volume;
        }
    }
}
