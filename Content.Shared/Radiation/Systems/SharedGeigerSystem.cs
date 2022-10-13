using Content.Shared.Examine;
using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

public abstract class SharedGeigerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedGeigerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, SharedGeigerComponent component, ExaminedEvent args)
    {
        if (!component.ShowExamine || !args.IsInDetailsRange)
            return;

        var rads = component.CurrentRadiation.ToString("N1");
        var color = RadsToColor(component.CurrentRadiation);
        var msg = Loc.GetString("geiger-component-examine",
            ("rads", rads), ("color", color));
        args.PushMarkup(msg);
    }

    public static GeigerDangerLevel RadsToLevel(float rads)
    {
        return rads switch
        {
            < 0.2f => GeigerDangerLevel.None,
            < 1f => GeigerDangerLevel.Low,
            < 3f => GeigerDangerLevel.Med,
            < 6f => GeigerDangerLevel.High,
            _ => GeigerDangerLevel.Extreme
        };
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

    public static Color RadsToColor(float rads)
    {
        var level = RadsToLevel(rads);
        return LevelToColor(level);
    }
}
