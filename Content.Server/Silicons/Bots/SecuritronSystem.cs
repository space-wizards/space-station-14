using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.CombatMode;
using Content.Shared.Silicons.Bots;
using Content.Shared.Silicons.Bots.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Silicons.Bots;

public sealed partial class SecuritronSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SecuritronComponent, HTNComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var securitron, out var htn, out var appearance))
        {
            var state = SecuritronVisualState.Online;

            if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode || HasComp<NPCMeleeCombatComponent>(uid))
                state = SecuritronVisualState.Combat;

            if (state == securitron.CurrentState)
                continue;

            securitron.CurrentState = state;

            _appearance.SetData(uid, SecuritronVisuals.State, state, appearance);
        }
    }
}
