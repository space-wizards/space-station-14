// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Wires;
using Content.Shared.SS220.Photocopier;
using Content.Shared.Wires;

namespace Content.Server.SS220.Photocopier;

[DataDefinition]
public sealed partial class PhotocopierBurnButtWireAction : ComponentWireAction<PhotocopierComponent>
{
    public override string Name { get; set; } = "wire-name-photocopier-burn";
    public override Color Color { get; set; } = Color.Red;
    public override object? StatusKey { get; } = BurnButtWireKey.StatusKey;
    public override StatusLightState? GetLightState(Wire wire, PhotocopierComponent component)
        => component.BurnsButts ? StatusLightState.BlinkingFast : StatusLightState.On;

    private PhotocopierSystem _photocopier = default!;

    public override void Initialize()
    {
        base.Initialize();

        _photocopier = EntityManager.System<PhotocopierSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        component.BurnsButts = true;
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        component.BurnsButts = false;
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        _photocopier.TryManuallyBurnButtOnTop(wire.Owner, component);
    }
}

[DataDefinition]
public sealed partial class PhotocopierSusFormsWireAction : ComponentWireAction<PhotocopierComponent>
{
    public override string Name { get; set; } = "wire-name-photocopier-contraband";
    public override Color Color { get; set; } = Color.Green;
    public override object? StatusKey { get; } = SusFormsWireKey.StatusKey;
    public override StatusLightState? GetLightState(Wire wire, PhotocopierComponent component)
        => component.SusFormsUnlocked ? StatusLightState.BlinkingSlow : StatusLightState.On;

    private PhotocopierSystem _photocopier = default!;

    public override void Initialize()
    {
        base.Initialize();

        _photocopier = EntityManager.System<PhotocopierSystem>();
    }

    public override bool Cut(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        _photocopier.SetContrabandFormsUnlocked(wire.Owner, component, true);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        _photocopier.SetContrabandFormsUnlocked(wire.Owner, component, false);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, PhotocopierComponent component)
    {
        _photocopier.SetContrabandFormsUnlocked(wire.Owner, component, !component.SusFormsUnlocked);
    }
}
