using Content.Shared.Emp;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.BarSign;

public sealed class BarSignSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<BarSignComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BarSignComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        Subs.BuiEvents<BarSignComponent>(BarSignUiKey.Key,
            subs =>
        {
            subs.Event<SetBarSignMessage>(OnSetBarSignMessage);
        });

        SubscribeLocalEvent<BarSignComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<BarSignComponent, BoundUserInterfaceMessageAttempt>(OnBoundUIAttempt);
    }

    private void OnMapInit(Entity<BarSignComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Current != null)
            return;

        var newPrototype = _random.Pick(GetAllBarSigns(_prototypeManager));
        SetBarSign(ent, newPrototype);
    }

    private void OnAfterAutoHandleState(Entity<BarSignComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // Update the UI if the component was changed.
        if (_ui.TryGetOpenUi(ent.Owner, BarSignUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnSetBarSignMessage(Entity<BarSignComponent> ent, ref SetBarSignMessage args)
    {
        if (!_prototypeManager.Resolve(args.Sign, out var signPrototype))
            return;

        if (signPrototype.Hidden)
            return; // Hidden signs cannot be selected from the BUI.

        SetBarSign(ent, signPrototype);
    }

    private void OnEmpPulse(Entity<BarSignComponent> ent, ref EmpPulseEvent args)
    {
        if (!_prototypeManager.Resolve(ent.Comp.Emped, out var empedPrototype))
            return;

        SetBarSign(ent, empedPrototype);
        args.Affected = true;
        args.Disabled = true;
    }

    private void OnBoundUIAttempt(Entity<BarSignComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (HasComp<EmpDisabledComponent>(ent))
            args.Cancel();
    }

    /// <summary>
    /// Set the sprite, name and description of the bar sign to a given <see cref="BarSignPrototype"/>.
    /// </summary>
    public void SetBarSign(Entity<BarSignComponent> ent, BarSignPrototype newPrototype)
    {
        if (ent.Comp.Current == newPrototype.ID)
            return;

        if (HasComp<EmpDisabledComponent>(ent))
            return;

        var meta = MetaData(ent);
        var name = Loc.GetString(newPrototype.Name);
        _metaData.SetEntityName(ent, name, meta);
        _metaData.SetEntityDescription(ent, Loc.GetString(newPrototype.Description), meta);
        _appearance.SetData(ent.Owner, BarSignVisuals.BarSignPrototype, newPrototype.ID);

        ent.Comp.Current = newPrototype.ID;
        Dirty(ent);

        // Predict updating the BUI if it's open.
        if (_ui.TryGetOpenUi(ent.Owner, BarSignUiKey.Key, out var bui))
            bui.Update();
    }

    /// <summary>
    /// Returns a list of all <see cref="BarSignPrototype"/>s that are not hidden.
    /// </summary>
    public static List<BarSignPrototype> GetAllBarSigns(IPrototypeManager prototypeManager)
    {
        return prototypeManager
            .EnumeratePrototypes<BarSignPrototype>()
            .Where(p => !p.Hidden)
            .ToList();
    }
}
