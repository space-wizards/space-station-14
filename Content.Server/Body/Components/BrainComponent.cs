using Content.Server.Body.Systems;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(BrainSystem))]
    public sealed class BrainComponent : Component
    {
    }
}
