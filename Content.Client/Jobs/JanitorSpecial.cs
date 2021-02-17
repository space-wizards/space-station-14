using Content.Shared.Roles;
using JetBrains.Annotations;

namespace Content.Client.Jobs
{
    [UsedImplicitly]
    public class JanitorSpecial : JobSpecial
    {
        // Dummy class that exists solely to avoid an exception on the client,
        // but allow the server-side counterpart to exist.
    }
}
