using Content.Shared.Atmos;

namespace Content.Shared.Body;

public sealed class ExhaleLocationEvent : EntityEventArgs
{
    public GasMixture? Gas;
}
