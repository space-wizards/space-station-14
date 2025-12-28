using Content.Shared.Administration;
using Content.Shared.Popups;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Mobs;

/// <summary>
///     Handles performing crit-specific actions.
/// </summary>
public abstract class SharedCritMobActionsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedQuickDialogSystem _quickDialog = default!;

    private const int MaxLastWordsLength = 30;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateActionsComponent, CritLastWordsEvent>(OnLastWords);
    }


    private void OnLastWords(Entity<MobStateActionsComponent> ent, ref CritLastWordsEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        var attachedEntity = actor.PlayerSession.AttachedEntity;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("action-name-crit-last-words"), "",
            (string lastWords) =>
            {
                // if a person is gibbed/deleted, they can't say last words
                if (Deleted(ent.Owner))
                    return;

                // Intentionally does not check for muteness
                if (attachedEntity != ent.Owner)
                    return;

                if (!_mobState.IsCritical(ent.Owner))
                    return;

                if (lastWords.Length > MaxLastWordsLength)
                {
                    lastWords = lastWords.Substring(0, MaxLastWordsLength);
                }
                lastWords += "...";

                RaiseNetworkEvent(new CritLastWordsSayEvent(lastWords));
            });

        args.Handled = true;
    }
}
