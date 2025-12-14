using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class VocalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VocalComponent, SexChangedEvent>(OnSexChanged);
        SubscribeLocalEvent<VocalComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<VocalComponent, ScreamActionEvent>(OnScreamAction);
    }

    /// <summary>
    /// Copy this component's datafields from one entity to another.
    /// This can't use CopyComp because of the ScreamActionEntity DataField, which should not be copied.
    /// <summary>
    public void CopyComponent(Entity<VocalComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp))
            return;

        var targetComp = EnsureComp<VocalComponent>(target);
        targetComp.Sounds = source.Comp.Sounds;
        targetComp.ScreamId = source.Comp.ScreamId;
        targetComp.Wilhelm = source.Comp.Wilhelm;
        targetComp.WilhelmProbability = source.Comp.WilhelmProbability;
        LoadSounds(target, targetComp);

        Dirty(target, targetComp);
    }

    private void OnMapInit(EntityUid uid, VocalComponent component, MapInitEvent args)
    {
        // try to add scream action when vocal comp added
        _actions.AddAction(uid, ref component.ScreamActionEntity, component.ScreamAction);
        LoadSounds(uid, component);
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        // remove scream action when component removed
        if (component.ScreamActionEntity != null)
        {
            _actions.RemoveAction(uid, component.ScreamActionEntity);
        }
    }

    private void OnSexChanged(EntityUid uid, VocalComponent component, SexChangedEvent args)
    {
        LoadSounds(uid, component, args.NewSex);
    }

    private void OnEmote(EntityUid uid, VocalComponent component, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        // snowflake case for wilhelm scream easter egg
        if (args.Emote.ID == component.ScreamId)
        {
            args.Handled = TryPlayScreamSound(uid, component);
            return;
        }

        if (component.EmoteSounds is not { } sounds)
            return;

        // just play regular sound based on emote proto
        args.Handled = _chat.TryPlayEmoteSound(uid, _proto.Index(sounds), args.Emote);
    }

    private void OnScreamAction(EntityUid uid, VocalComponent component, ScreamActionEvent args)
    {
        if (args.Handled)
            return;

        _chat.TryEmoteWithChat(uid, component.ScreamId);
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

        return _chat.TryPlayEmoteSound(uid, _proto.Index(sounds), component.ScreamId);
    }

    private void LoadSounds(EntityUid uid, VocalComponent component, Sex? sex = null)
    {
        if (component.Sounds == null)
            return;

        sex ??= CompOrNull<HumanoidAppearanceComponent>(uid)?.Sex ?? Sex.Unsexed;

        if (!component.Sounds.TryGetValue(sex.Value, out var protoId))
            return;

        if (!_proto.HasIndex(protoId))
            return;

        component.EmoteSounds = protoId;
    }
}
