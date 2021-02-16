using Content.Shared.Chemistry;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface ITileReaction : IExposeData
    {
        ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume);
    }
}
