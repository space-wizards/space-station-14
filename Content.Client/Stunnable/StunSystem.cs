using Content.Shared.Stunnable;

namespace Content.Client.Stunnable
{
    public sealed class StunSystem : SharedStunSystem
    {
        // TODO: Clientside prediction
        // DoAfter mis-predicts on client hard when in shared so it's gonna need it's own special system because it hates me.
    }
}
