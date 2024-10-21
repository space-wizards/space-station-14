using Content.Shared.DeltaV.Roles;

namespace Content.Shared.Roles;

public abstract partial class SharedRoleSystem
{
    private void InitializeDeltaV()
    {
        SubscribeAntagEvents<ListeningPostRoleComponent>();
        SubscribeAntagEvents<RecruiterRoleComponent>();
    }
}
