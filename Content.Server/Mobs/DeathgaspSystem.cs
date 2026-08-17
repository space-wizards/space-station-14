using Content.Server.Chat.Systems;
using Content.Shared.Mobs;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Mobs;

/// <summary>
/// A system that handles death gasps, an emote a character makes when they die.
/// </summary>
/// <seealso cref="DeathgaspComponent"/>
public sealed partial class DeathgaspSystem : SharedDeathgaspSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<DeathgaspComponent> ent, ref MobStateChangedEvent args)
    {
        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(ent, ent.Comp);
    }

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public override bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (_statusEffects.HasEffectComp<MutedStatusEffectComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
