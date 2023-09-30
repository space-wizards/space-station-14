using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class ArchaicAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArchaicAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, ArchaicAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "archaic");
        return msg;
    }

    private void OnAccentGet(EntityUid uid, ArchaicAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
