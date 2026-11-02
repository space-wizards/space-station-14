using Content.Client.UserInterface.Controls;
using Content.Shared.Actions.Components;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Spells;
using Content.Shared.Popups;
using Robust.Shared.Maths;

namespace Content.Client.CF.Spells;

/// <summary>
/// Force Barrage and any other multi-bolt spell ask how many bolts to fire before aiming.
/// One spell slot is spent for the cast, however many bolts you pick.
/// </summary>
public sealed class CFSpellBoltMenuSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private CFActionPoolSystem _actions = default!;
    [Dependency] private CFSpellSystem _spells = default!;

    private SimpleRadialMenu? _menu;

    /// <summary>
    /// Opens the 1 / 2 / 3 choice. Returns false when this spell fires a single bolt and should aim immediately.
    /// </summary>
    public bool TryOffer(Entity<ActionComponent> action, Action begin)
    {
        if (!TryComp(action.Owner, out CFSpellComponent? spell) || spell.MaxBolts <= 1)
            return false;

        if (action.Comp.AttachedEntity is not { } user)
            return false;

        if (!_spells.HasSlot(user, spell))
        {
            _popup.PopupEntity(Loc.GetString("cf-spell-no-slots"), user, user);
            return true;
        }

        var ready = _actions.ReadyCount(user);
        if (ready < 1)
        {
            _popup.PopupEntity(Loc.GetString("cf-action-not-enough"), user, user);
            return true;
        }

        var max = Math.Min(spell.MaxBolts, ready);
        var options = new List<RadialMenuOptionBase>(max);
        for (var n = 1; n <= max; n++)
        {
            var count = n;
            options.Add(new RadialMenuActionOption<int>(picked =>
            {
                spell.ChosenBolts = picked;
                spell.VolleyLeft = picked;
                spell.VolleyArmed = false;
                spell.PayThisShot = false;
                begin();
            }, count)
            {
                Text = count.ToString(),
                ToolTip = Loc.GetString("cf-spell-bolt-choice", ("count", count)),
                Order = count,
                BackgroundColor = new Color(36, 72, 140, 190),
                HoverBackgroundColor = new Color(90, 150, 210, 220),
            });
        }

        _menu?.Close();
        _menu = new SimpleRadialMenu();
        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
        return true;
    }
}
