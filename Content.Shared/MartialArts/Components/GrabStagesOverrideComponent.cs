using Content.Shared.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Movement.Pulling.Systems;

namespace Content.Shared.MartialArts;

public abstract partial class GrabStagesOverrideComponent : Component
{
    public GrabStage StartingStage = GrabStage.Hard;
}
