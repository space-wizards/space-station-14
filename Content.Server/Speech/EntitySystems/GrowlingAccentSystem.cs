using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class GrowlingAccentSystem : EntitySystem
{
	[Dependency] private readonly IRobustRandom _random = default!;
	
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrowlingAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GrowlingAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // r := rr | rrr
        message = Regex.Replace(message, "r+", _random.Pick(new List<string>() { "rr", "rrr" }));
        // R := Rr | Rrr
        message = Regex.Replace(message, "R+", _random.Pick(new List<string>() { "Rr", "Rrr" }));
		
		// р := рр | ррр
        message = Regex.Replace(message, "р+", _random.Pick(new List<string>() { "рр", "рррр" }));
        // Р := Рр | Ррр
        message = Regex.Replace(message, "Р+", _random.Pick(new List<string>() { "Рр", "Ррр" }));

        args.Message = message;
    }
}
