using Content.Shared.Interaction.Components;
using Content.Shared.LegallyDistinctSpaceFerret;
using Robust.Client.Animations;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.LegallyDistinctSpaceFerret;

public sealed class LegallyDistinctSpaceFerretSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DoABackFlipEvent>(OnBackflipEvent);
        SubscribeNetworkEvent<GoEepyEvent>(OnEepyEvent);
    }

    public void OnBackflipEvent(DoABackFlipEvent args)
    {
        if (!TryGetEntity(args.Backflipper, out var uid))
        {
            return;
        }

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(0.66),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(0), 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.33f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 0.33f),
                    }
                }
            }
        };

        _animation.Play(uid.Value, animation, "backflip");
        _audio.PlayEntity(new SoundPathSpecifier("/Audio/Animals/slugclap.ogg"), Filter.Local(), uid.Value, false);
    }

    public void OnEepyEvent(GoEepyEvent args)
    {
        if (!TryGetEntity(args.Eepier, out var uid))
        {
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var comp))
        {
            return;
        }

        comp.LayerSetState(0, "legallydistinctspaceferret_eepy");
    }
}
