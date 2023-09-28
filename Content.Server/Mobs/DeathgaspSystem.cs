using Content.Server.Chat.Systems;
using Content.Server.Speech.Muting;
using Content.Shared.Mobs;
using Content.Server.EUI;
using Robust.Server.Player;
using Content.Server.SS220.DeathReminder.DeathReminderEui;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;


namespace Content.Server.Mobs;


/// <see cref="DeathgaspComponent"/>
public sealed class DeathgaspSystem: EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathgaspComponent, MobStateChangedEvent>(OnMobStateChanged);
    }


    private void OnMobStateChanged(EntityUid uid, DeathgaspComponent component, MobStateChangedEvent args)
    {

        if (args.NewMobState == MobState.Dead)
        {
            if (TryComp<MindContainerComponent>(uid, out var mindContainer))
            {
                if (mindContainer.Mind is { } mind)
                {
                    if (TryComp<MindComponent>(mind, out var mindComp))
                    {
                        if (mindComp.UserId != null && _playerManager.TryGetSessionById(mindComp.UserId.Value, out var client))
                        {
                            _euiManager.OpenEui(new DeathReminderEui(), client);
                        }
                    }
                }
            }
        }

        // don't deathgasp if they arent going straight from crit to dead
        if (args.NewMobState != MobState.Dead || args.OldMobState != MobState.Critical)
            return;

        Deathgasp(uid, component);
    }

    /// <summary>
    ///     Causes an entity to perform their deathgasp emote, if they have one.
    /// </summary>
    public bool Deathgasp(EntityUid uid, DeathgaspComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (HasComp<MutedComponent>(uid))
            return false;

        _chat.TryEmoteWithChat(uid, component.Prototype, ignoreActionBlocker: true);

        return true;
    }
}
