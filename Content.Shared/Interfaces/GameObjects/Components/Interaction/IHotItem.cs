using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface gives components hot quality when they are used.
    /// E.g if you hold a lit match or a welder then it will be hot,
    /// presuming match is lit or the welder is on respectively.
    /// However say you hold an item that is always hot like lava rock,
    /// it will be permanently hot.
    /// </summary>
    public interface IHotItem
    {
        bool IsCurrentlyHot();
    }
}
