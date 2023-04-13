using Content.Server.Defusable.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Salvage;
using Content.Shared.Defusable;
using Content.Shared.Examine;

namespace Content.Server.Defusable.Systems;

/// <inheritdoc/>
public sealed class DefusableSystem : SharedDefusableSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DefusableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, DefusableComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!comp.BombUsable) {
            args.PushMarkup(Loc.GetString("defusable-examine-defused"));
        }
        else if (comp.BombLive)
        {
            args.PushMarkup(Loc.GetString("defusable-examine-live", ("time", comp.TimeUntilExplosion.ToString())));
        }
        else
        {
            args.PushMarkup(Loc.GetString("defusable-examine-inactive"));
        }
    }

    public void TryStartCountdown(EntityUid uid, DefusableComponent comp)
    {
        // todo: handle countdown
        // also might want to have admin logs
        if (!comp.BombUsable)
            return;

        comp.BombLive = true;

        Logger.Debug("it begins");

        UpdateAppearance(uid, comp);
    }

    public void TryDetonateBomb(EntityUid uid, DefusableComponent comp)
    {
        // todo: boom??? lol?
        // also might want to have admin logs
        if (!comp.BombLive)
            return;

        Logger.Debug("boom");
        _explosionSystem.TriggerExplosive(uid);
        QueueDel(uid);

        UpdateAppearance(uid, comp);
    }

    public void DefuseBomb(EntityUid uid, DefusableComponent comp)
    {
        // todo: defusing lmfao
        // also might want to have admin logs

        Logger.Debug("counter terrorists win");
        comp.BombLive = false;
        comp.BombUsable = false; // fry the circuitry

        UpdateAppearance(uid, comp);
    }

    public void Update()
    {
        // todo: handle bombs
    }

    private void UpdateAppearance(EntityUid uid, DefusableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        // _appearance.SetData(uid, DefusableVisuals.Active, component.Wires == MagnetStateType.Attaching);
        // _appearance.SetData(uid, DefusableVisuals.ActiveWires, component.Wires == MagnetStateType.Holding);
        // _appearance.SetData(uid, DefusableVisuals.Inactive, component.Wires == MagnetStateType.CoolingDown);
        // _appearance.SetData(uid, DefusableVisuals.InactiveWires, component.Wires == MagnetStateType.Detaching);
    }
}
