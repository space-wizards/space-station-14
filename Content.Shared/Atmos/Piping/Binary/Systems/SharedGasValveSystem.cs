using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using GasValveComponent = Content.Shared.Atmos.Piping.Binary.Components.GasValveComponent;

namespace Content.Shared.Atmos.Piping.Binary.Systems;

public abstract class SharedGasValveSystem : EntitySystem
{

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasValveComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<GasValveComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GasValveComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(EntityUid uid, GasValveComponent component, ComponentStartup args)
    {
        // We call set in startup so it sets the appearance, node state, etc.
        Set(uid, component, component.Open);
    }

    public virtual void Set(EntityUid uid, GasValveComponent component, bool value)
    {
        component.Open = value;
        Dirty(uid, component);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, FilterVisuals.Enabled, component.Open, appearance);
        }
    }

    public void Toggle(EntityUid uid, GasValveComponent component)
    {
        Set(uid, component, !component.Open);
    }

    private void OnActivate(EntityUid uid, GasValveComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Toggle(uid, component);
        _audio.PlayPredicted(component.ValveSound, uid, args.User, AudioParams.Default.WithVariation(0.25f));
        args.Handled = true;
    }

    private void OnExamined(Entity<GasValveComponent> ent, ref ExaminedEvent args)
    {
        var valve = ent.Comp;
        if (!Transform(ent).Anchored)
            return;

        if (Loc.TryGetString("gas-valve-system-examined", out var str,
                ("statusColor", valve.Open ? "green" : "orange"),
                ("open", valve.Open)))
        {
            args.PushMarkup(str);
        }
    }
}
