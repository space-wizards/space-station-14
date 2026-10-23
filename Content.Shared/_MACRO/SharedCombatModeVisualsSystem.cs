using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.CombatMode;

public abstract partial class SharedCombatModeVisualsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeVisualsComponent, ToggleCombatActionEvent>(
            OnCombatToggle,
            after: [typeof(SharedCombatModeSystem)]);
        SubscribeLocalEvent<CombatModeVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnCombatToggle(Entity<CombatModeVisualsComponent> ent, ref ToggleCombatActionEvent args)
    {
        if (TryComp<CombatModeComponent>(ent, out var combat))
            _appearance.SetData(ent, CombatModeVisualLayers.Combat, combat.IsInCombatMode);
    }

    private void OnMobStateChanged(Entity<CombatModeVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
            _appearance.SetData(ent, CombatModeVisualLayers.Combat, false);
    }

    [Serializable, NetSerializable]
    public enum CombatModeVisualLayers : byte
    {
        Combat
    }
}
