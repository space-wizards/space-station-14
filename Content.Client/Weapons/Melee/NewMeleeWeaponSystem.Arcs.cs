using Content.Client.Weapons.Melee.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Melee;

public sealed partial class NewMeleeWeaponSystem
{
    private void InitializeArcs()
    {
        SubscribeLocalEvent<WeaponArcVisualsComponent, ComponentStartup>(OnArcStartup);
    }

    private void OnArcStartup(EntityUid uid, WeaponArcVisualsComponent component, ComponentStartup args)
    {
        var animation = GetFadeout(uid);

        if (animation == null)
        {
            _sawmill.Error($"Unable to get animation component for melee arc {ToPrettyString(uid)}");
            return;
        }

        _animation.Play(uid, animation, "melee-arc");
    }

    private Animation? GetFadeout(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return null;

        var length = 0.15f;

        return new()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), length)
                    }
                }
            }
        };
    }
}
