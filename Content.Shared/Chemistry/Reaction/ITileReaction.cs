#nullable enable
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Map;

namespace Content.Shared.Chemistry.Reaction
{
    public interface ITileReaction
    {
        ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume);
    }
}
