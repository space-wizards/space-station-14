using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Kitchen
{
    /// <summary>
    /// Tag component that denotes an entity as Juiceable
    /// </summary>
    [RegisterComponent]
    public class JuiceableComponent : Component
    {
        public override string Name => "Juicable";
        //TODO: add juice result.

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            
        }

    }
}
