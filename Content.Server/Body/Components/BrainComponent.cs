using Content.Server.Body.Systems;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(BrainSystem))]
    public sealed partial class BrainComponent : Component
    {
    }
}
