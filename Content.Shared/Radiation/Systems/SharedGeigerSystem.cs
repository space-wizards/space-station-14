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
