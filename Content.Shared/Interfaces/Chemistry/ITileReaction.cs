#nullable enable
using Content.Shared.Chemistry;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface ITileReaction
    {
        ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume);
    }
}
