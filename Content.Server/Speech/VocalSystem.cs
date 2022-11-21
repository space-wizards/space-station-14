using Content.Server.Chat.Systems;
using Content.Server.Humanoid;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech;

/// <summary>
///     Fer Screamin
/// </summary>
/// <remarks>
///     Or I guess other vocalizations, like laughing. If fun is ever legalized on the station.
/// </remarks>
public sealed class VocalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<VocalComponent, SexChangedEvent>(OnSexChanged);

        SubscribeLocalEvent<VocalComponent, ScreamActionEvent>(OnActionPerform);
        SubscribeLocalEvent<VocalComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnEmote(EntityUid uid, VocalComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        // snowflake case for wilhelm scream easter egg
        if (args.Emote.ID == component.ScreamId)
        {
            args.Handled = TryScream(uid, component);
            return;
        }

        // just play regular sound based on emote proto
        args.Handled = TryPlayEmoteSound(uid, component, args.Emote.ID);
    }

    private bool TryPlayEmoteSound(EntityUid uid, VocalComponent component, string emoteId)
    {
        var proto = component.EmoteSounds;
        if (proto == null || !proto.Sounds.TryGetValue(emoteId, out var sound))
            return false;

        var param = proto.Params ?? sound.Params;
        _audio.PlayPvs(sound, uid, param);
        return true;
    }

    private bool TryScream(EntityUid uid, VocalComponent component)
    {
        if (_random.Prob(component.WilhelmProbability))
        {
            _audio.PlayPvs(component.Wilhelm, uid, component.Wilhelm.Params);
            return true;
        }

        return TryPlayEmoteSound(uid, component, component.ScreamId);
    }

    private void OnSexChanged(EntityUid uid, VocalComponent component, SexChangedEvent args)
    {
        LoadSounds(uid, component);
    }

    private void OnStartup(EntityUid uid, VocalComponent component, ComponentStartup args)
    {
        if (component.ScreamAction == null
            && _proto.TryIndex(component.ActionId, out InstantActionPrototype? act))
        {
            component.ScreamAction = new(act);
        }

        if (component.ScreamAction != null)
            _actions.AddAction(uid, component.ScreamAction, null);

        LoadSounds(uid, component);
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        if (component.ScreamAction != null)
            _actions.RemoveAction(uid, component.ScreamAction);
    }

    private void OnActionPerform(EntityUid uid, VocalComponent component, ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryScream(uid, component);
    }

    private void LoadSounds(EntityUid uid, VocalComponent component, Sex? sex = null)
    {
        if (component.SoundsBySex == null)
            return;

        sex ??= CompOrNull<HumanoidComponent>(uid)?.Sex ?? Sex.Unsexed;

        if (!component.SoundsBySex.TryGetValue(sex.Value, out var protoId))
            return;
        if (!_proto.TryIndex(protoId, out EmoteSoundsPrototype? proto))
            return;

        component.EmoteSounds = proto;
    }

    /*public bool TryScream(EntityUid uid, VocalComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (!_blocker.CanSpeak(uid))
            return false;



        if (_random.Prob(component.WilhelmProbability))
        {
            SoundSystem.Play(component.Wilhelm.GetSound(), Filter.Pvs(uid), uid, component.AudioParams);
            return true;
        }

        var scale = (float) _random.NextGaussian(1, VocalComponent.Variation);
        var pitchedParams = component.AudioParams.WithPitchScale(scale);



        return true;
    }*/
}
