using Content.Shared.Magic;
using Content.Shared.Magic.Events;

namespace Content.Client.Magic;

public sealed class MagicSystem : SharedMagicSystem
{

    public override void OnVoidApplause(VoidApplauseSpellEvent ev)
    {
        base.OnVoidApplause(ev);

        var perfXForm = Transform(ev.Performer);
        var targetXForm = Transform(ev.Target);

        Spawn(ev.Effect, perfXForm.Coordinates);
        Spawn(ev.Effect, targetXForm.Coordinates);
    }
}
