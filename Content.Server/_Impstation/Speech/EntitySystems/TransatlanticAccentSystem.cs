using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class TransatlanticAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransatlanticAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, TransatlanticAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "transatlantic_accent");

        args.Message = message;
    }
}
