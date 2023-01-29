using Content.Shared.AI;

namespace Content.Client.AI
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedAIComponent))]
    public sealed class AIComponent : SharedAIComponent
    {
    }
}
