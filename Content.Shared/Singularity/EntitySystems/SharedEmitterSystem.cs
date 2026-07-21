using Content.Shared.Examine;
using Content.Shared.Singularity.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Singularity.EntitySystems;

public abstract partial class SharedEmitterSystem : EntitySystem
{
    [Dependency] protected BatteryWeaponFireModesSystem FireMode = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmitterComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, EmitterComponent component, ExaminedEvent args)
    {
        if (!FireMode.TryGetFireMode((uid, null), out var fireMode))
            return;

        var proto = ProtoMan.Index<EntityPrototype>(fireMode.Prototype);
        args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
    }
}
