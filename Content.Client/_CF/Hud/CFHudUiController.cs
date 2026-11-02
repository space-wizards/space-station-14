using Content.Client.CF.Hud;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Character;
using Content.Shared.CF.Combat;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.CF.Hud;

/// <summary>
/// Fills the health bottles and spell pips from the local creature's Pathfinder components.
/// </summary>
[UsedImplicitly]
public sealed class CFHudUiController : UIController
{
    [Dependency] private IPlayerManager _player = default!;

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (UIManager.GetActiveUIWidgetOrNull<CFGameHud>() is not { } hud)
            return;

        // The stock hotbar strip (hands, status panels, old slot row) stays hidden.
        // Storage windows still parent to that widget, above this HUD.
        if (UIManager.GetActiveUIWidgetOrNull<HotbarGui>() is { } hotbar
            && hotbar.HandContainer.Parent is { } row)
            row.Visible = false;

        if (_player.LocalEntity is not { } player
            || !EntityManager.TryGetComponent<CFCombatantComponent>(player, out var combatant))
        {
            hud.Ball.Visible = false;
            hud.ActionPips.Set(null);
            hud.Spells.SetBook(null);
            hud.ApplySpellVisibility();
            return;
        }

        hud.Ball.Visible = true;
        var offGuard = EntityManager.TryGetComponent(player, out CFOffGuardComponent? flanked) && flanked.Active;
        hud.Ball.Set(combatant.CurrentHp, combatant.MaxHp, combatant.TemporaryHp, combatant.ArmorClass, offGuard);
        EntityManager.TryGetComponent<CFActionPoolComponent>(player, out var actions);
        hud.ActionPips.Set(actions);
        EntityManager.TryGetComponent<CFSpellbookComponent>(player, out var book);
        hud.Spells.SetBook(book);
        hud.ApplySpellVisibility();
    }
}
