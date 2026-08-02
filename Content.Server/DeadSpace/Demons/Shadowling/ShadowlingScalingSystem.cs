// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.Shadowling;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingScalingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingComponent, MapInitEvent>(OnShadowlingSpawned);
    }

    private void OnShadowlingSpawned(EntityUid uid, ShadowlingComponent component, MapInitEvent args)
    {
        var ruleQuery = EntityQueryEnumerator<ShadowlingRuleComponent>();
        float scale = 0f;
        bool found = false;
        while (ruleQuery.MoveNext(out var ruleComp))
        {
            scale = ruleComp.Scale;
            found = true;
            break;
        }

        if (!found)
            return;

        if (TryComp<ShadowlingFreezingVeinsComponent>(uid, out var veins))
            veins.RequiredSlaves = (int)Math.Round(MathHelper.Lerp(veins.MinRequiredSlaves, veins.MaxRequiredSlaves, scale));

        if (TryComp<ShadowlingScreechComponent>(uid, out var screech))
            screech.RequiredSlaves = (int)Math.Round(MathHelper.Lerp(screech.MinRequiredSlaves, screech.MaxRequiredSlaves, scale));

        if (TryComp<ShadowlingMindShieldBreakComponent>(uid, out var mindShieldBreak))
            mindShieldBreak.RequiredSlaves = (int)Math.Round(MathHelper.Lerp(mindShieldBreak.MinRequiredSlaves, mindShieldBreak.MaxRequiredSlaves, scale));

        if (TryComp<ShadowlingBlackMedComponent>(uid, out var blackMed))
            blackMed.RequiredSlaves = (int)Math.Round(MathHelper.Lerp(blackMed.MinRequiredSlaves, blackMed.MaxRequiredSlaves, scale));

        if (TryComp<ShadowlingAscendanceComponent>(uid, out var ascendance))
            ascendance.RequiredSlaves = (int)Math.Round(MathHelper.Lerp(ascendance.MinRequiredSlaves, ascendance.MaxRequiredSlaves, scale));
    }
}