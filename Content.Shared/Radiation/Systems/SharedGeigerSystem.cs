using Content.Shared.Examine;
using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

public abstract class SharedGeigerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, GeigerComponent component, ExaminedEvent args)
    {
        if (!component.ShowExamine || !component.IsEnabled || !args.IsInDetailsRange)
            return;

        var currentRads = component.CurrentRadiation;
        var rads = currentRads.ToString("N1");
        var color = LevelToColor(component.DangerLevel);
        var msg = Loc.GetString("geiger-component-examine",
            ("rads", rads), ("color", color));
        args.PushMarkup(msg);
    }

    public static Color LevelToColor(GeigerDangerLevel level)
    {
        switch (level)
        {
            case GeigerDangerLevel.None:
                return Color.Green;
            case GeigerDangerLevel.Low:
                return Color.Yellow;
            case GeigerDangerLevel.Med:
                return Color.DarkOrange;
            case GeigerDangerLevel.High:
            case GeigerDangerLevel.Extreme:
                return Color.Red;
            default:
                return Color.White;
        }
    }
}
