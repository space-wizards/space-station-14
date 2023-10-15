using Content.Shared.Atmos;

namespace Content.Shared.Body;

public sealed class InhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}
