using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A disposable, single-use smokable.
    /// </summary>
    [RegisterComponent, Friend(typeof(SmokingSystem))]
    public class CigarComponent : Component
    {
        public override string Name => "Cigar";
    }
}
