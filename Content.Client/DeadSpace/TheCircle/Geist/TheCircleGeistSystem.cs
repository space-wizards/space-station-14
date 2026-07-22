// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.UserInterface.Controls;
using Content.Shared.DeadSpace.TheCircle.Geist;
using Robust.Shared.Timing;

namespace Content.Client.DeadSpace.TheCircle.Geist;

public sealed class TheCircleGeistSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private SimpleRadialMenu? _menu;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TheCircleGeistComponent, OpenGeistInvisibilityMenuEvent>(OnOpenMenu);
    }

    private void OnOpenMenu(Entity<TheCircleGeistComponent> ent, ref OpenGeistInvisibilityMenuEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = true;
        _menu?.Close();
        _menu = new SimpleRadialMenu();
        var buttons = new List<RadialMenuOptionBase>();

        foreach (var option in ent.Comp.Modes)
        {
            var readyAt = option.Mode switch
            {
                GeistInvisibilityMode.Escape => ent.Comp.EscapeReadyAt,
                GeistInvisibilityMode.Phase => ent.Comp.PhaseReadyAt,
                _ => ent.Comp.StationaryReadyAt,
            };
            var remaining = readyAt - _timing.CurTime;
            var tooltip = Loc.GetString(option.Tooltip);
            if (remaining > TimeSpan.Zero)
                tooltip += "\n" + Loc.GetString("geist-invisibility-mode-cooldown", ("seconds", Math.Ceiling(remaining.TotalSeconds)));

            buttons.Add(new RadialMenuActionOption<GeistInvisibilityMode>(
                mode =>
                {
                    if (readyAt <= _timing.CurTime)
                        SelectMode(ent.Owner, mode);
                }, option.Mode)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(option.Icon),
                ToolTip = tooltip,
                BackgroundColor = remaining > TimeSpan.Zero ? Color.DarkSlateGray : null,
            });
        }

        _menu.SetButtons(buttons);
        _menu.OpenCentered();
    }

    private void SelectMode(EntityUid geist, GeistInvisibilityMode mode)
    {
        RaiseNetworkEvent(new SelectGeistInvisibilityModeEvent(GetNetEntity(geist), mode));
        _menu?.Close();
    }
}
