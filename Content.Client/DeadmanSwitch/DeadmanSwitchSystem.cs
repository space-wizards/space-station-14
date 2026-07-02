using Content.Shared.DeadmanSwitch;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Client.DeadmanSwitch;

public sealed class DeadmanSwitchSystem : SharedDeadmanSwitchSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void ToggleInHandFeedback(Entity<DeadmanSwitchComponent?> ent, EntityUid? user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Resolve(ent, ref ent.Comp))
            return;

        if (user != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.Armed ? "deadman-on-activate" : "deadman-on-deactivate", ("name", ent)), ent, user.Value);

        _audio.PlayPvs(ent.Comp.SwitchSound, ent);
    }
}
