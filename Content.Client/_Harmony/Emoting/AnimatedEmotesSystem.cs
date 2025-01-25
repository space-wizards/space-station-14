// Original code by whateverusername0 from Goob-Station at commit 3022db4
// Available at: https://github.com/Goob-Station/Goob-Station/blob/3022db48e89ff00b762004767e7850023df3ee97/Content.Client/_Goobstation/Emoting/AnimatedEmotesSystem.cs
// Rewritten by Jajsha to remove duplicate code.

using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Client.GameObjects;
using System.Numerics;
using Robust.Shared.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared._Harmony.Emoting;

namespace Content.Client._Harmony.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    [Dependency] private readonly AnimationPlayerSystem _anim = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;

    // Dict to hold all emote animations
    private readonly Dictionary<string, AnimationTrackComponentProperty> _emoteAnimations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<AnimatedEmotesComponent, AnimatedEmoteEvent>(OnAnimatedEmote);

        InitializeEmoteAnimations();
    }

    /// <summary>
    /// Initializes the emote animations for the system.
    /// </summary>
    /// <remarks>
    /// This method sets up the animation tracks for different emotes.
    /// The emote animations are stored in a dictionary with the key being the emote name.
    /// </remarks>
    public void InitializeEmoteAnimations()
    {
        // flip emote
        _emoteAnimations["Flip"] = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Rotation),
            InterpolationMode = AnimationInterpolationMode.Linear,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 0.25f),
            }
        };

        // spin emote
        _emoteAnimations["Spin"] = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(TransformComponent),
            Property = nameof(TransformComponent.LocalRotation),
            InterpolationMode = AnimationInterpolationMode.Linear,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(90), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(270), 0.075f),
                new AnimationTrackProperty.KeyFrame(Angle.Zero, 0.075f),
            }
        };

        // jump emote
        _emoteAnimations["Jump"] = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(SpriteComponent),
            Property = nameof(SpriteComponent.Offset),
            InterpolationMode = AnimationInterpolationMode.Cubic,
            KeyFrames =
            {
                new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                new AnimationTrackProperty.KeyFrame(new Vector2(0, .35f), 0.125f),
                new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.125f),
            }
        };
    }

    public void PlayEmote(EntityUid uid, Animation anim, string animationKey = "emoteAnimKeyId")
    {
        if (_anim.HasRunningAnimation(uid, animationKey))
            return;

        _anim.Play(uid, anim, animationKey);
    }

    private void OnHandleState(EntityUid uid, AnimatedEmotesComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AnimatedEmotesComponentState state)
            return;

        if (!_prot.TryIndex<EmotePrototype>(state.Emote, out var emote))
            return;

        if (!emote.Animated || emote.AnimationLength == null)
            return;

        var ev = new AnimatedEmoteEvent { Emote = emote.ID, Length = emote.AnimationLength.Value };
        RaiseLocalEvent(uid, ev);
    }

    private void OnAnimatedEmote(Entity<AnimatedEmotesComponent> ent, ref AnimatedEmoteEvent args)
    {
        if (args.Length <= 0 || !_emoteAnimations.ContainsKey(args.Emote))
            return;

        var anim = new Animation
        {
            Length = TimeSpan.FromMilliseconds(args.Length),
            AnimationTracks = { _emoteAnimations[args.Emote] }
        };
        PlayEmote(ent, anim);
    }
}
