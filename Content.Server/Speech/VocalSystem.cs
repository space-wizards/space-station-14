using Content.Server.Speech.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance;
using Content.Shared.CharacterAppearance.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
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
        _actions.AddAction(uid, component.Action, null);
    }

    private void OnShutdown(EntityUid uid, VocalComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.Action);
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

        // Currently this requires humanoid appearance & doesn't have any sort of fall-back or gender-neutral scream.
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid))
            return false;

        if (_random.Prob(.01f))
        {
            SoundSystem.Play(Filter.Pvs(uid), component.Wilhelm.GetSound(), uid, component.AudioParams);
            return true;
        }

        var scale = (float) _random.NextGaussian(1, VocalComponent.Variation);
        var pitchedParams = component.AudioParams.WithPitchScale(scale);

        switch (humanoid.Sex)
        {
            case Sex.Male:
                SoundSystem.Play(Filter.Pvs(uid), component.MaleScream.GetSound(), uid, pitchedParams);
                break;
            case Sex.Female:
                SoundSystem.Play(Filter.Pvs(uid), component.FemaleScream.GetSound(), uid, pitchedParams);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return true;
    }
}
