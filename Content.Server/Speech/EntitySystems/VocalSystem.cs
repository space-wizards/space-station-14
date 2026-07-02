using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class VocalSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VocalComponent, VoiceChangedEvent>(OnVoiceChanged);
        SubscribeLocalEvent<VocalComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<VocalComponent, EmoteActionEvent>(OnEmoteAction);
    }

    /// <summary>
    /// Copy this component's datafields from one entity to another.
    /// This can't use CopyComp because of the ScreamActionEntity DataField, which should not be copied.
    /// </summary>
    public void CopyComponent(Entity<VocalComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp))
            return;

        var targetComp = EnsureComp<VocalComponent>(target);
        targetComp.ScreamId = source.Comp.ScreamId;
        targetComp.Wilhelm = source.Comp.Wilhelm;
        targetComp.WilhelmProbability = source.Comp.WilhelmProbability;
        LoadSounds(target, targetComp);

        Dirty(target, targetComp);
    }

    private void OnMapInit(EntityUid uid, VocalComponent component, MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        _actions.AddAction(uid, ref component.EmoteActionEntity, component.EmoteAction);
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        // remove scream action when component removed
        if (component.EmoteActionEntity != null)
        {
            _actions.RemoveAction(uid, component.EmoteActionEntity);
        }
    }

    private void OnVoiceChanged(EntityUid uid, VocalComponent component, VoiceChangedEvent args)
    {
        LoadSounds(uid, component, args.NewVoice);
    }

    private void OnEmote(EntityUid uid, VocalComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        // snowflake case for wilhelm scream easter egg
        if (args.Emote == component.ScreamId)
        {
            args.Handled = TryPlayScreamSound(uid, component);
            return;
        }

        if (component.EmoteSounds is not { } sounds)
            return;

        // just play regular sound based on emote proto
        args.Handled = _chat.TryPlayEmoteSound(uid, ProtoMan.Index(sounds), args.Emote);
    }

    private void OnEmoteAction(EntityUid uid, VocalComponent component, EmoteActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TryEmoteWithChat(uid, args.Emote);
        args.Handled = true;
    }

    private bool TryPlayScreamSound(EntityUid uid, VocalComponent component)
    {
        if (_random.Prob(component.WilhelmProbability))
        {
            _audio.PlayPvs(component.Wilhelm, uid, component.Wilhelm.Params);
            return true;
        }

        if (component.EmoteSounds is not { } sounds)
            return false;

        return _chat.TryPlayEmoteSound(uid, ProtoMan.Index(sounds), component.ScreamId);
    }

    /// <summary>
    /// This only works on Humanoids. Mobs should have emoteSounds on <see cref="VocalComponent"/> set directly instead.
    /// </summary>
    private void LoadSounds(EntityUid uid, VocalComponent component, ProtoId<EmoteSoundsPrototype>? protoId = null)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        protoId ??= humanoid.Voice;

        if (!ProtoMan.HasIndex(protoId))
            return;

        component.EmoteSounds = protoId;
    }
}
