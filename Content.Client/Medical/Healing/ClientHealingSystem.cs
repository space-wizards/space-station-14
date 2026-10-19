using System.Numerics;
using Content.Client.Animations;
using Content.Shared.Medical.Healing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using System.Numerics;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Spawners;
using static Robust.Client.Animations.AnimationTrackProperty;
using Content.Client.Movement.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Client.Medical.Healing;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ClientHealingSystem : HealingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityPickupAnimationSystem _entityPickupAnimation = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AnimationPlayerSystem _animations = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void PlayHealAnimation(EntityUid uid)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        HealAnimation(uid);
    }

    /// <summary>
    ///     Animates a clone of an entity moving from one point to another before
    ///     being deleted.
    ///     Used when the player picks up an entity.
    /// </summary>
    public void HealAnimation(EntityUid uid, Color? color = null)
    {
        color ??= Color.Green;
        if (Deleted(uid))
            return;

        var metadata = MetaData(uid);

        if (IsPaused(uid, metadata))
            return;

        if (!TryComp(uid, out SpriteComponent? sprite0))
        {
            Log.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", metadata.EntityName, nameof(SpriteComponent));
            return;
        }

        var animations = Comp<AnimationPlayerComponent>(uid);
        var curColor = sprite0.Color;

        _animations.Play(new Entity<AnimationPlayerComponent>(uid, animations),
            new Animation
        {
            Length = TimeSpan.FromMilliseconds(300),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(curColor, 0),
                        new KeyFrame(color, 0.125f),
                        new KeyFrame(curColor, 0.150f),
                        new KeyFrame(curColor, 0.200f),
                        new KeyFrame(curColor, 0.250f),
                        new KeyFrame(curColor, 0.300f),
                    },
                },
            },
        },
            "heal_anim");
    }
}
