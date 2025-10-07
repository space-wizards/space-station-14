using Content.Shared.Examine;
using Content.Shared.Singularity.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Singularity.EntitySystems;

public abstract class SharedEmitterSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly BatteryWeaponFireModesSystem FireMode = default!;

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

        var proto = _prototype.Index<EntityPrototype>(fireMode.Prototype);
        args.PushMarkup(Loc.GetString("emitter-component-current-type", ("type", proto.Name)));
    }
}
