using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Random.Helpers;
using Robust.Server.Audio;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ghost;

// Handlers for interactions with the GhostBooEvent.
public sealed partial class GhostSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private FlammableSystem _flammable = default!;

    [Dependency] private EntityQuery<FlammableComponent> _flammableQuery;
    [Dependency] private EntityQuery<BlinkingPoweredLightComponent> _blinkingQuery;

    [SubscribeLocalEvent]
    private void OnSpeakerBoo(Entity<SpookySpeakerComponent> ent, ref GhostBooEvent args)
    {
        // Check if already handled, or too intense.
        if (!IsIntensityPermitted(args, ent.Comp.Intensity))
            return;

        // Only activate sometimes, so groups don't all trigger together
        if (!_random.Prob(ent.Comp.SpeakChance))
            return;

        var curTime = _timing.CurTime;
        // Enforce a delay between messages to prevent spam
        if (curTime < ent.Comp.NextSpeakTime)
            return;

        if (!ProtoMan.Resolve(ent.Comp.MessageSet, out var messages))
            return;

        // Grab a random localized message from the set
        var message = _random.Pick(messages);
        // Chatcode moment: messages starting with '.' are considered radio messages unless prefixed with '>'
        // So this is a stupid trick to make the "...Oooo"-style messages work.
        message = '>' + message;
        // Say the message
        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, hideChat: true);

        // Set the delay for the next message
        ent.Comp.NextSpeakTime = curTime + ent.Comp.Cooldown;

        args.ResponseIntensity = ent.Comp.Intensity;
    }

    [SubscribeLocalEvent]
    private void OnExtinguishBoo(Entity<SpookyExtinguishableComponent> ent, ref GhostBooEvent args)
    {
        // Check if already handled, or too intense.
        if (!IsIntensityPermitted(args, ent.Comp.Intensity))
            return;

        // Check if we can extinguish the entity.
        if (!_flammableQuery.TryComp(ent, out var flammable))
            return;

        // Check if we need to extinguish this entity.
        if (!_random.Prob(ent.Comp.ExtinguishChance))
            return;

        if (!_flammable.Extinguish(ent, flammable))
            return;

        if (ent.Comp.ExtinguishSound != null)
            _audio.PlayPvs(ent.Comp.ExtinguishSound, ent);

        args.ResponseIntensity = GhostBooIntensity.Subtle;
    }

    [SubscribeLocalEvent]
    private void OnPoweredLightBoo(Entity<SpookyPoweredLightComponent> ent, ref GhostBooEvent args)
    {
        // Already handled?
        if (!IsIntensityPermitted(args, ent.Comp.BooIntensity))
            return;

        // Is the light already blinking?
        if (_blinkingQuery.HasComp(ent))
            return;

        // Check cooldown first to prevent abuse.
        var curTime = _timing.CurTime;
        if (curTime < ent.Comp.NextGhostBlink)
            return;

        ent.Comp.NextGhostBlink = curTime + ent.Comp.GhostBlinkingCooldown;

        var blinkingComp = EnsureComp<BlinkingPoweredLightComponent>(ent);
        blinkingComp.StopBlinkingTime = curTime + ent.Comp.GhostBlinkingTime;
        Dirty(ent, blinkingComp);

        args.ResponseIntensity = ent.Comp.BooIntensity;
    }

    private bool IsIntensityPermitted(GhostBooEvent args, GhostBooIntensity targetIntensity)
    {
        return args.ResponseIntensity == GhostBooIntensity.None
            && args.AllowedIntensity >= targetIntensity;
    }
}
