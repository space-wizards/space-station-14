using Content.Shared.Destructible;
using Content.Shared.Emag.Systems;
using Content.Shared.LawChips.Judge;
using Robust.Server.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.LawChips.Judge;

public sealed class JudgeInterfaceSystem : SharedJudgeInterfaceSystem
{
    [Dependency] AppearanceSystem _appearance = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JudgeInterfaceComponent, BreakageEventArgs>(OnBreakage);
        SubscribeLocalEvent<JudgeInterfaceComponent, GotEmaggedEvent>(OnHacked);
    }

    private void OnHacked(Entity<JudgeInterfaceComponent> ent, ref GotEmaggedEvent args)
    {
        //Broken and hacked J.U.D.G.Es can't be hacked... again
        if (ent.Comp.Status != JudgeInterfaceStatus.Normal)
            return;

        args.Handled = true;


        ent.Comp.Status = JudgeInterfaceStatus.Hacked;

        UpdateVisualState(ent.Owner, ent.Comp);
    }

    private void OnBreakage(Entity<JudgeInterfaceComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Status = JudgeInterfaceStatus.Broken;

        UpdateVisualState(ent.Owner, ent.Comp);
    }

    private void UpdateVisualState(EntityUid uid, JudgeInterfaceComponent component)
    {
        //Conveniently, the appearance component uses the same enum used to store component state as a key.

        _appearance.SetData(uid, JudgeInterfaceVisuals.DeviceState, component.Status);
    }
}
