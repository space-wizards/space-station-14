using Content.Server.Chat.Systems;
using Content.Shared.Mobs;
using Content.Shared.Speech.Muting;

namespace Content.Server.Mobs.Systems;

public sealed class MobsSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<UnconsciousComponent, MobStateChangedEvent>(OnUnconscious);
    }

    private void OnDeath(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(uid, component);
    }

    private void OnUnconscious(EntityUid uid, UnconsciousComponent component, MobStateChangedEvent args)
    {
        // don't unconscious if they arent going straight from alive to crit
        if (args.NewMobState != MobState.Critical || args.OldMobState != MobState.Alive)
            return;

        Unconscious(uid, component);
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

    /// <summary>
    ///     Causes an entity to perform their unconscious emote, if they have one.
    /// </summary>
    public bool Unconscious(EntityUid uid, UnconsciousComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (HasComp<MutedComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
