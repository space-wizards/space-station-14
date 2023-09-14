using Content.Server.Chat.Systems;
using Content.Server.Speech.Muting;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.Mobs;

/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem: EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(uid, component);
    }

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (HasComp<MutedComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
