using Content.Shared.DeviceLinking.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking.Systems;

public sealed partial class TwoWayLeverSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private const string LeftToggleImage = "rotate_ccw.svg.192dpi.png";
    private const string RightToggleImage = "rotate_cw.svg.192dpi.png";

    [SubscribeLocalEvent]
    private void OnInit(Entity<TwoWayLeverComponent> ent, ref ComponentInit args)
    {
        _signalSystem.EnsureSourcePorts(ent.Owner, ent.Comp.LeftPort, ent.Comp.RightPort, ent.Comp.MiddlePort);
    }

    [SubscribeLocalEvent]
    private void OnActivated(Entity<TwoWayLeverComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            TwoWayLeverState.Middle => ent.Comp.NextSignalLeft ? TwoWayLeverState.Left : TwoWayLeverState.Right,
            TwoWayLeverState.Right => TwoWayLeverState.Middle,
            TwoWayLeverState.Left => TwoWayLeverState.Middle,
            _ => throw new ArgumentOutOfRangeException()
        };

        StateChanged(ent);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnGetInteractionVerbs(Entity<TwoWayLeverComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || (args.Hands == null))
            return;

        var disabled = ent.Comp.State == TwoWayLeverState.Left;
        InteractionVerb verbLeft = new()
        {
            Act = () =>
            {
                ent.Comp.State = ent.Comp.State switch
                {
                    TwoWayLeverState.Middle => TwoWayLeverState.Left,
                    TwoWayLeverState.Right => TwoWayLeverState.Middle,
                    _ => throw new ArgumentOutOfRangeException()
                };
                StateChanged(ent);
            },
            Category = VerbCategory.Lever,
            Message = disabled ? Loc.GetString("two-way-lever-cant") : null,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new ($"/Textures/Interface/VerbIcons/{LeftToggleImage}")),
            Text = Loc.GetString("two-way-lever-left"),
        };

        args.Verbs.Add(verbLeft);

        disabled = ent.Comp.State == TwoWayLeverState.Right;
        InteractionVerb verbRight = new()
        {
            Act = () =>
            {
                ent.Comp.State = ent.Comp.State switch
                {
                    TwoWayLeverState.Left => TwoWayLeverState.Middle,
                    TwoWayLeverState.Middle => TwoWayLeverState.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };
                StateChanged(ent);
            },
            Category = VerbCategory.Lever,
            Message = disabled ? Loc.GetString("two-way-lever-cant") : null,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new ($"/Textures/Interface/VerbIcons/{RightToggleImage}")),
            Text = Loc.GetString("two-way-lever-right"),
        };

        args.Verbs.Add(verbRight);
    }

    private void StateChanged(Entity<TwoWayLeverComponent> ent)
    {
        if (ent.Comp.State == TwoWayLeverState.Middle)
            ent.Comp.NextSignalLeft = !ent.Comp.NextSignalLeft;

        _appearance.SetData(ent.Owner, TwoWayLeverVisuals.State, ent.Comp.State);

        var port = ent.Comp.State switch
        {
            TwoWayLeverState.Left => ent.Comp.LeftPort,
            TwoWayLeverState.Right => ent.Comp.RightPort,
            TwoWayLeverState.Middle => ent.Comp.MiddlePort,
            _ => throw new ArgumentOutOfRangeException()
        };

        Dirty(ent);
        _signalSystem.InvokePort(ent.Owner, port);
    }
}
