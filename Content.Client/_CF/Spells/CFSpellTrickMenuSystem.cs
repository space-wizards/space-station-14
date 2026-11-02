using Content.Client.UserInterface.Controls;
using Content.Shared.Actions.Components;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Spells;
using Content.Shared.Popups;
using Robust.Shared.Maths;

namespace Content.Client.CF.Spells;

/// <summary>
/// Prestidigitation asks for Food, Lift, Make, or Tidy before the cast.
/// Food, Lift, and Tidy then wait for a click. Make happens immediately.
/// </summary>
public sealed class CFSpellTrickMenuSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CFActionPoolSystem _actions = default!;
    [Dependency] private CFSpellSystem _spells = default!;

    private SimpleRadialMenu? _menu;

    public bool TryOffer(Entity<ActionComponent> action, Action begin)
    {
        if (!TryComp(action.Owner, out CFSpellComponent? spell) || !spell.TrickMenu)
            return false;

        if (action.Comp.AttachedEntity is not { } user)
            return false;

        if (!_spells.HasSlot(user, spell))
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-no-slots"), user, user);
            return true;
        }

        var cost = _actions.ActionCount(action);
        if (_actions.ReadyCount(user) < cost)
        {
            _popup.PopupEntity(Loc.GetString("cf-action-not-enough"), user, user);
            return true;
        }

        var options = new List<RadialMenuOptionBase>
        {
            Slice(action.Owner, CFPrestidigitationTrick.Food, "cf-spell-trick-food", "cf-spell-trick-food-tip", spell, begin),
            Slice(action.Owner, CFPrestidigitationTrick.Lift, "cf-spell-trick-lift", "cf-spell-trick-lift-tip", spell, begin),
            Slice(action.Owner, CFPrestidigitationTrick.Make, "cf-spell-trick-make", "cf-spell-trick-make-tip", spell, begin),
            Slice(action.Owner, CFPrestidigitationTrick.Tidy, "cf-spell-trick-tidy", "cf-spell-trick-tidy-tip", spell, begin),
        };

        _menu?.Close();
        _menu = new SimpleRadialMenu();
        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
        return true;
    }

    private RadialMenuActionOption<byte> Slice(
        EntityUid action,
        CFPrestidigitationTrick trick,
        string name,
        string tip,
        CFSpellComponent spell,
        Action begin)
    {
        var value = (byte) trick;
        return new RadialMenuActionOption<byte>(picked =>
        {
            spell.ChosenTrick = picked;
            if (trick == CFPrestidigitationTrick.Make)
            {
                RaisePredictiveEvent(new CFPrestidigitationMakeEvent(GetNetEntity(action)));
                return;
            }

            begin();
        }, value)
        {
            Text = Loc.GetString(name),
            ToolTip = Loc.GetString(tip),
            Order = value,
            BackgroundColor = new Color(72, 48, 120, 190),
            HoverBackgroundColor = new Color(140, 110, 190, 220),
        };
    }
}
