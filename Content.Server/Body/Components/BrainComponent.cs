using Content.Server.Body.Systems;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(BrainSystem))]
    public sealed class BrainComponent : Component
    {
    }
}
