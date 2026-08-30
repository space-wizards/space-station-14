using Content.Shared.Administration.Logs;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Speech.Components;
using Robust.Shared.Player;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class MeleeSpeechSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnComponentMapInit(Entity<MeleeSpeechComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent, ref ent.Comp.ConfigureActionEntity, ent.Comp.ConfigureAction, ent);
    }

    [SubscribeLocalEvent]
    private void OnGetActions(Entity<MeleeSpeechComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ConfigureActionEntity, ent.Comp.ConfigureAction);
    }

    [SubscribeLocalEvent]
    private void OnBattlecryChanged(Entity<MeleeSpeechComponent> ent, ref MeleeSpeechBattlecryChangedMessage args)
    {
        var battlecry = args.Battlecry;
        if (battlecry.Length > ent.Comp.MaxBattlecryLength)
            battlecry = battlecry[..ent.Comp.MaxBattlecryLength];

        TryChangeBattlecry(ent.AsNullable(), battlecry);
    }

    /// <summary>
    /// Attempts to open the Battlecry UI.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnConfigureAction(Entity<MeleeSpeechComponent> ent, ref MeleeSpeechConfigureActionEvent args)
    {
        TryOpenUi(args.Performer, ent.AsNullable());
    }

    /// <summary>
    /// Attempts to open the Battlecry UI for a user.
    /// </summary>
    public void TryOpenUi(EntityUid user, Entity<MeleeSpeechComponent?> source)
    {
        if (!Resolve(source, ref source.Comp))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(source.Owner, MeleeSpeechUiKey.Key, actor.PlayerSession);
    }

    /// <summary>
    /// Attempts to change the battlecry of an entity.
    /// Returns true/false.
    /// </summary>
    /// <remarks>
    /// Logs changes to an entity's battlecry
    /// </remarks>
    public bool TryChangeBattlecry(Entity<MeleeSpeechComponent?> ent, string? battlecry)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        battlecry = !string.IsNullOrWhiteSpace(battlecry) ? battlecry.Trim() : null;

        if (ent.Comp.Battlecry == battlecry)
            return true;

        ent.Comp.Battlecry = battlecry;
        Dirty(ent);
        _adminLogger.Add(LogType.ItemConfigure,
            LogImpact.Medium,
            $" {ToPrettyString(ent):entity}'s battlecry has been changed to {battlecry}");

        return true;
    }
}
