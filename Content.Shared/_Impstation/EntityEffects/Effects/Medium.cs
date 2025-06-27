using Content.Shared._Impstation.Ghost;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.EntityEffects.Effects;

public sealed partial class Medium : EventEntityEffect<Medium>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Grants whoever drinks this the ability to see ghosts for a while";
    }
}
