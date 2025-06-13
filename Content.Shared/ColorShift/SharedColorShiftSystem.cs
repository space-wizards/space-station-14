using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ColorShift;

/// <summary>
/// Base shared class for slime hue shifting for networking purposes
/// </summary>
public abstract class SharedColorShiftSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ColorShifterComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ColorShifterComponent, OpenColorShiftEvent>(OpenHueShift);
    }

    private void OnComponentStartup(Entity<ColorShifterComponent> ent, ref ComponentStartup args)
    {
        // try to add hueshift action
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OpenHueShift(Entity<ColorShifterComponent> ent, ref OpenColorShiftEvent args)
    {
        _ui.OpenUi(ent.Owner, ColorShifterComponent.ColorShiftUiKey.Key, true);
    }
}

/// <summary>
/// Network message to request colorshift.
/// </summary>
[NetSerializable, Serializable]
public sealed class PleaseHueShiftNetworkMessage : BoundUserInterfaceMessage
{
    public readonly float Hue;
    public readonly float Saturation;
    public readonly float Value;

    public PleaseHueShiftNetworkMessage(float h, float s, float v)
    {
        Hue = h;
        Saturation = s;
        Value = v;
    }
}

/// <summary>
/// Do after event for color shifting.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HueShiftDoAfterEvent : DoAfterEvent
{
    public Color NewColor;

    public HueShiftDoAfterEvent(Color newColor)
    {
        NewColor = newColor;
    }

    public override DoAfterEvent Clone() => this;
}
