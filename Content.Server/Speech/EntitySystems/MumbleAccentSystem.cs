using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class MumbleAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MumbleAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, MumbleAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "mumble");

        return msg;
    }

    private void OnAccentGet(EntityUid uid, MumbleAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
