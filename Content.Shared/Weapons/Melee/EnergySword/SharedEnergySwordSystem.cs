using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Melee.EnergySword;

public abstract class SharedEnergySwordSystem : EntitySystem
{
    [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergySwordComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EnergySwordComponent, EnergySwordColorMessage>(OnPickedColor);
    }

    // Used to pick a random color for the blade on map init.
    private void OnMapInit(Entity<EnergySwordComponent> entity, ref MapInitEvent args)
    {
        if (entity.Comp.ColorOptions.Count != 0)
        {
            entity.Comp.ActivatedColor = _random.Pick(entity.Comp.ColorOptions);
            Dirty(entity);
        }

        if (!TryComp(entity, out AppearanceComponent? appearanceComponent))
            return;

        _appearance.SetData(entity, ToggleableVisuals.Color, entity.Comp.ActivatedColor!, appearanceComponent);
    }

    // Used to make the blade multicolored when using a multitool on it.
    private void OnInteractUsing(Entity<EnergySwordComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_toolSystem.HasQuality(args.Used, SharedToolSystem.PulseQuality))
            return;

        args.Handled = true;

        OpenInterface(entity, args.User);



        Dirty(entity);
    }

    private void OnPickedColor(Entity<EnergySwordComponent> ent, ref EnergySwordColorMessage args)
    {

        //Is the color in the authorized colors or is it rgb?
        if (ent.Comp.ColorOptions.Contains(args.ChoosenColor) || args.RGB)
        {
            if (!TryComp(ent, out AppearanceComponent? appearanceComponent))
                return;
            if (args.RGB)
            {
                ent.Comp.Hacked = true;
                var rgb = EnsureComp<RgbLightControllerComponent>(ent);
                _rgbSystem.SetCycleRate(ent, ent.Comp.CycleRate, rgb);
            }
            else
            {
                ent.Comp.ActivatedColor = args.ChoosenColor;
                ent.Comp.Hacked = false;
                RemComp<RgbLightControllerComponent>(ent);
                Dirty(ent);
                _appearance.SetData(ent, ToggleableVisuals.Color, ent.Comp.ActivatedColor, appearanceComponent);

            }
        }
    }

    protected virtual void OpenInterface(Entity<EnergySwordComponent> ent, EntityUid actor)
    {
    }
}
