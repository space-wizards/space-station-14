using Content.Server.Chemistry.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private EntityQuery<AnchoredBlurComponent> _blurQuery = default!;

    private void TurnOn(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
        EnsureComp<ActiveSolutionHeaterComponent>(uid);
        ToggleBlurSetting(uid, true);
    }

    public bool TryTurnOn(EntityUid uid, ItemPlacerComponent? placer = null)
    {
        if (!Resolve(uid, ref placer))
            return false;

        if (placer.PlacedEntities.Count <= 0 || !_powerReceiver.IsPowered(uid))
            return false;

        TurnOn(uid);
        return true;
    }

    public void TurnOff(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, false);
        RemComp<ActiveSolutionHeaterComponent>(uid);
        ToggleBlurSetting(uid, false);
    }

    [SubscribeLocalEvent]
    private void OnPowerChanged(Entity<SolutionHeaterComponent> entity, ref PowerChangedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(entity);
        if (args.Powered && placer.PlacedEntities.Count > 0)
        {
            TurnOn(entity);
        }
        else
        {
            TurnOff(entity);
        }
    }

    [SubscribeLocalEvent]
    private void OnItemPlaced(Entity<SolutionHeaterComponent> entity, ref ItemPlacedEvent args)
    {
        TryTurnOn(entity);
    }

    [SubscribeLocalEvent]
    private void OnItemRemoved(Entity<SolutionHeaterComponent> entity, ref ItemRemovedEvent args)
    {
        var placer = Comp<ItemPlacerComponent>(entity);
        if (placer.PlacedEntities.Count == 0) // Last entity was removed
            TurnOff(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out _, out _, out var heater, out var placer))
        {
            var energy = heater.HeatPerSecond * frameTime;
            foreach (var heatingEntity in placer.PlacedEntities)
            {
                foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(heatingEntity))
                {
                    _solutionContainer.AddThermalEnergy(soln, energy);
                }
            }
        }
    }

    private void ToggleBlurSetting(EntityUid uid, bool toggle)
    {
        if (!_blurQuery.TryComp(uid, out var blur))
            return;

        if (toggle)
            blur.Strength = blur.TargetStrength;
        else
            blur.Strength = 0.0f;

        Dirty(uid, blur);
    }
}
