///using Content.Shared.Inventory;
using Content.Server.Body.Systems;
namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Used in breath tool with a smoke gas filter.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SmokeFilterSystem))]
    [ComponentProtoName("FilterMask")]
    public sealed partial class FilterMaskComponent : Component
    {
        [DataField("IsUsed")]
        public bool IsActive = false;
    }
}
