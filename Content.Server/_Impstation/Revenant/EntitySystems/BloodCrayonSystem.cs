using Content.Shared.Interaction;
using Content.Server.Revenant.Components;
using Content.Server.Crayon;
using Content.Shared.Revenant.Components;
using Content.Server.Popups;

namespace Content.Server.Revenant.EntitySystems;

public sealed class BloodCrayonSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RevenantSystem _revenant = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCrayonComponent, AfterInteractEvent>(OnCrayonUse, before: [typeof(CrayonSystem)]);
    }

    private void OnCrayonUse(EntityUid uid, BloodCrayonComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<RevenantComponent>(args.User, out var revenant))
            return;

        if (!_revenant.ChangeEssenceAmount(args.User, -revenant.BloodWritingCost, allowDeath: false))
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, args.User);
            args.Handled = true;
            return;
        }
    }
}
