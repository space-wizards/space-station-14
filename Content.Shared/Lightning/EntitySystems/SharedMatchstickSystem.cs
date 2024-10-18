using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedMatchstickSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MatchstickComponent, IsHotEvent>(OnIsHotEvent);
    }

    private void OnInteractUsing(Entity<MatchstickComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.CurrentState != SmokableState.Unlit)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot)
            return;

        Ignite(ent, args.User);
        args.Handled = true;
    }

    private void OnIsHotEvent(EntityUid uid, MatchstickComponent component, IsHotEvent args)
    {
        args.IsHot = component.CurrentState == SmokableState.Lit;
    }

    public void Ignite(Entity<MatchstickComponent> matchstick, EntityUid user)
    {
        // Play Sound
        _audio.PlayPredicted(matchstick.Comp.IgniteSound, matchstick, user, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));

        // Change state
        SetState(matchstick, matchstick.Comp, SmokableState.Lit);
        matchstick.Comp.TimeMatchWillBurnOut = _timing.CurTime + TimeSpan.FromSeconds(matchstick.Comp.Duration);

    }

    protected void SetState(EntityUid uid, MatchstickComponent component, SmokableState value)
    {
        component.CurrentState = value;

        if (_lights.TryGetLight(uid, out var pointLightComponent))
        {
            _lights.SetEnabled(uid, component.CurrentState == SmokableState.Lit, pointLightComponent);
        }

        if (EntityManager.TryGetComponent(uid, out ItemComponent? item))
        {
            switch (component.CurrentState)
            {
                case SmokableState.Lit:
                    _item.SetHeldPrefix(uid, "lit", component: item);
                    break;
                default:
                    _item.SetHeldPrefix(uid, "unlit", component: item);
                    break;
            }
        }

        if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, SmokingVisuals.Smoking, component.CurrentState, appearance);
        }
    }
}
