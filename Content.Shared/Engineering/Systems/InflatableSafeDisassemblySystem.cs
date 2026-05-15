using Content.Shared.Engineering.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Balloon;

namespace Content.Shared.Engineering.Systems;

/// <summary>
/// Implements <see cref="InflatableSafeDisassemblyComponent"/>
/// </summary>
public sealed class InflatableSafeDisassemblySystem : EntitySystem
{
    [Dependency] private readonly DisassembleOnAltVerbSystem _disassembleOnAltVerbSystem = null!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InflatableSafeDisassemblyComponent, InteractUsingEvent>(InteractHandler);
    }

    private void InteractHandler(Entity<InflatableSafeDisassemblyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<BalloonPopperComponent>(args.Used))
            return;

        _popupSystem.PopupPredicted(
            Loc.GetString("inflatable-safe-disassembly", ("item", args.Used), ("target", ent.Owner)),
            ent,
            args.User);

        _disassembleOnAltVerbSystem.StartDisassembly((ent, Comp<DisassembleOnAltVerbComponent>(ent)), args.User);
        args.Handled = true;
    }
}
