using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.Components
{
    /// <summary>
    /// Handles conditional sprite visuals,
    /// e.g. if a belt can have 4 different layers visible depending if a stun baton etc are insreted.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SharedItemMapperSystem))]
    public sealed class ItemMapperComponent : Component
    {
        [DataField("mapLayers")] public readonly Dictionary<string, SharedMapLayerData> MapLayers = new();

        [DataField("sprite")] public ResourcePath? RSIPath;

        public readonly List<string> SpriteLayers = new();
    }
}
