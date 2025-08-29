// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CauseInfectionDead : EventEntityEffect<CauseInfectionDead>
{
    [DataField]
    public InfectionDeadStrainData StrainData = new InfectionDeadStrainData();

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-infection-dead", ("chance", Probability));
}
