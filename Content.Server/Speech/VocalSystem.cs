using Content.Server.Humanoid;
using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VocalComponent, ScreamActionEvent>(OnActionPerform);
        SubscribeLocalEvent<VocalComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VocalComponent, ComponentShutdown>(OnShutdown);
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

    public bool TryScream(EntityUid uid, VocalComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (!_blocker.CanSpeak(uid))
            return false;

        var sex = CompOrNull<HumanoidComponent>(uid)?.Sex ?? Sex.Unsexed;

        if (_random.Prob(component.WilhelmProbability))
        {
            SoundSystem.Play(component.Wilhelm.GetSound(), Filter.Pvs(uid), uid, component.AudioParams);
            return true;
        }

        var scale = (float) _random.NextGaussian(1, VocalComponent.Variation);
        var pitchedParams = component.AudioParams.WithPitchScale(scale);

        switch (sex)
        {
            case Sex.Male:
                SoundSystem.Play(component.MaleScream.GetSound(), Filter.Pvs(uid), uid, pitchedParams);
                break;
            case Sex.Female:
                SoundSystem.Play(component.FemaleScream.GetSound(), Filter.Pvs(uid), uid, pitchedParams);
                break;
            default:
                SoundSystem.Play(component.UnsexedScream.GetSound(), Filter.Pvs(uid), uid, pitchedParams);
                break;
        }

        return true;
    }
}
