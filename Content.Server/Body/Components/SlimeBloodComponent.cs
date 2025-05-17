using Content.Server.Body.Systems;

namespace Content.Server.Body.Components
{
    /// <summary>
    /// Used by the SlimeSystem to respond
    /// to BloodColorOverrideEvents.
    /// </summary>
    [RegisterComponent, Access(typeof(SlimeBloodSystem))]
    public sealed partial class SlimeBloodComponent : Component
    {
    }
}
