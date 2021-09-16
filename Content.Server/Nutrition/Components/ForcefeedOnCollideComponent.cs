using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nutrition.Components
{
    /// <summary>
    ///     A food item with this component will be forcefully fed to anyone
    /// </summary>
    [RegisterComponent, Friend(typeof(ForcefeedOnCollideSystem))]
    public class ForcefeedOnCollideComponent : Component
    {
        public override string Name => "ForcefeedOnCollide";

        /// <summary>
        ///     Since this component is primarily used by the pneumatic cannon, which adds this comp on throw start
        ///     and wants to remove it on throw end, this is set to false. However, you're free to change it if you want
        ///     something that can -always- be forcefed on collide, or something.
        /// </summary>
        [DataField("removeOnThrowEnd")]
        public bool RemoveOnThrowEnd = true;
    }
}
