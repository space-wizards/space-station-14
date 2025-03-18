using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class RelentlessPositivitySystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    private static readonly Regex RegexContainsDoubleSpaces = new(@"  ");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RelentlessPositivityComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, RelentlessPositivityComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "relentless_positivity");
        message = RegexContainsDoubleSpaces.Replace(message, " ");

        args.Message = message;
    }
}
