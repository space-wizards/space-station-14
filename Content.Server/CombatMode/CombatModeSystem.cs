using Content.Shared.CombatMode;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, ComponentGetState>(OnGetState);
    }

    private void OnGetState(EntityUid uid, CombatModeComponent component, ref ComponentGetState args)
    {
        args.State = new CombatModeComponentState(component.IsInCombatMode, component.ActiveZone);
    }
}
