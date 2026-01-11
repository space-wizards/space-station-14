using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Speech.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Speech.EntitySystems;

public sealed class MeleeSpeechSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeSpeechComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<MeleeSpeechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MeleeSpeechComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<MeleeSpeechComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechBattlecryChangedMessage>(OnBattlecryChanged);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechConfigureActionEvent>(OnConfigureAction);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeHitEvent>(OnSpeechHit);
    }

    // Update the UI if the battlecry was changed.
    private void OnAfterAutoHandleState(Entity<MeleeSpeechComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    private void UpdateUi(Entity<MeleeSpeechComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, MeleeSpeechUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnMapInit(Entity<MeleeSpeechComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ConfigureAction == null)
            return;

        _actions.AddAction(ent.Owner, ref ent.Comp.ConfigureActionEntity, ent.Comp.ConfigureAction);
        Dirty(ent);
    }

    private void OnGetItemActions(Entity<MeleeSpeechComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ConfigureAction == null)
            return;

        args.AddAction(ref ent.Comp.ConfigureActionEntity, ent.Comp.ConfigureAction);
        Dirty(ent);
    }

    private void OnShutdown(Entity<MeleeSpeechComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ConfigureActionEntity);
    }

    private void OnBattlecryChanged(Entity<MeleeSpeechComponent> ent, ref MeleeSpeechBattlecryChangedMessage args)
    {
        TryChangeBattlecry(ent.AsNullable(), args.Battlecry);
    }

    private void OnConfigureAction(Entity<MeleeSpeechComponent> ent, ref MeleeSpeechConfigureActionEvent args)
    {
        _ui.TryToggleUi(ent.Owner, MeleeSpeechUiKey.Key, args.Performer);
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

        if (!string.IsNullOrWhiteSpace(battlecry))
        {
            battlecry = battlecry.Trim();
            if (battlecry.Length > ent.Comp.MaxBattlecryLength)
                battlecry = battlecry[..ent.Comp.MaxBattlecryLength];
        }
        else
        {
            battlecry = null;
        }

        if (ent.Comp.Battlecry == battlecry)
            return true;

        ent.Comp.Battlecry = battlecry;
        Dirty(ent);
        UpdateUi(ent!);
        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $" {ToPrettyString(ent.Owner):entity}'s battlecry has been changed to {battlecry}");
        return true;
    }

    private void OnSpeechHit(Entity<MeleeSpeechComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        if (ent.Comp.Battlecry != null) // If the battlecry is set to empty, don't speak.
        {
            // Speech that isn't sent to chat or adminlogs.
            _chat.TrySendInGameICMessage(
                args.User,
                ent.Comp.Battlecry,
                InGameICChatType.Speak,
                hideLog: true,
                hideChat: true,
                checkRadioPrefix: false);
        }
    }
}
