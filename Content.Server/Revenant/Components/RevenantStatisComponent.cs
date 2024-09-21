using Content.Server.Revenant.EntitySystems;
using Content.Shared.Revenant.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Revenant.Components;

[RegisterComponent]
[Access(typeof(RevenantStasisSystem))]
[NetworkedComponent]
public sealed partial class RevenantStasisComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Entity<RevenantComponent> Revenant;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan StasisDuration = TimeSpan.FromSeconds(60);

    public RevenantStasisComponent(TimeSpan stasisDuration, Entity<RevenantComponent> revenant)
    {
        StasisDuration = stasisDuration;
        Revenant = revenant;
    }
}
