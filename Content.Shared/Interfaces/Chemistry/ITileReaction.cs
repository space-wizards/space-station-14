using Content.Shared.Chemistry;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces.Chemistry
{
    public interface ITileReaction : IExposeData
    {
        ReagentUnit TileReact(TileRef tile, ReagentPrototype reagent, ReagentUnit reactVolume);
    }
}
