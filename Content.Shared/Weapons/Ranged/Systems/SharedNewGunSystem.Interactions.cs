using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedGunSystem
{
    private void OnExamine(EntityUid uid, GunComponent component, ExaminedEvent args)
    {
        var selectColor = component.SelectedMode == SelectiveFire.Safety ? SafetyExamineColor : ModeExamineColor;
        args.PushMarkup($"Current selected fire mode is [color={selectColor}]{component.SelectedMode}[/color].");
        args.PushMarkup($"Fire rate is [color={FireRateExamineColor}]{component.FireRate}[/color] per second.");
    }

    private void OnAltVerb(EntityUid uid, GunComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.SelectedMode == component.AvailableModes)
            return;

        var nextMode = GetNextMode(component);

        AlternativeVerb verb = new()
        {
            Act = () => SelectFire(component, nextMode, args.User),
            Text = $"Change to {nextMode}",
            IconTexture = "/Textures/Interface/VerbIcons/fold.svg.192dpi.png",
        };

        args.Verbs.Add(verb);
    }

    private SelectiveFire GetNextMode(GunComponent component)
    {
        var modes = new List<SelectiveFire>();

        foreach (var mode in Enum.GetValues<SelectiveFire>())
        {
            if ((mode & component.AvailableModes) == 0x0) continue;
            modes.Add(mode);
        }

        var index = modes.IndexOf(component.SelectedMode);
        return modes[(index + 1) % modes.Count];
    }

    public void SelectFire(GunComponent component, SelectiveFire fire, EntityUid? user = null)
    {
        if (component.SelectedMode == fire) return;

        DebugTools.Assert((component.AvailableModes  & fire) != 0x0);
        component.SelectedMode = fire;
        var curTime = Timing.CurTime;
        var cooldown = TimeSpan.FromSeconds(InteractNextFire);

        if (component.NextFire < curTime)
            component.NextFire = curTime + cooldown;
        else
            component.NextFire += cooldown;

        PlaySound(component.Owner, component.SoundModeToggle?.GetSound(), user);
        Popup($"Selected {fire}", component, user);

        if (component.SelectModeAction != null)
        {
            var nextMode = GetNextMode(component);
            UpdateSelectModeAction(component.SelectModeAction, nextMode);
        }

        Dirty(component);
    }

    /// <summary>
    /// Cycles the gun's <see cref="SelectiveFire"/> to the next available one.
    /// </summary>
    public void CycleFire(GunComponent component, EntityUid? user = null)
    {
        // Noop
        if (component.SelectedMode == component.AvailableModes) return;

        DebugTools.Assert((component.AvailableModes & component.SelectedMode) == component.SelectedMode);
        var nextMode = GetNextMode(component);
        SelectFire(component, nextMode, user);
    }

    private void OnGetActions(EntityUid uid, GunComponent component, GetItemActionsEvent args)
    {
        var action = GetSelectModeAction(component);

        if (action == null) return;

        args.Actions.Add(action);
        component.SelectModeAction = action;
    }

    private void UpdateSelectModeAction(InstantAction? action, SelectiveFire mode)
    {
        if (action == null) return;

        // For the action we'll show our current mode as the icon I guess
        switch (mode)
        {
            case SelectiveFire.Safety:
                action.Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png"));
                break;
            case SelectiveFire.SemiAuto:
                action.Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png"));
                break;
            case SelectiveFire.Burst:
                action.Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png"));
                break;
            case SelectiveFire.FullAuto:
                action.Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png"));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    $"No implemented select mode action for {mode}!");
        }

        action.Description = $"Cycle the mode for this gun to {mode}";
        action.Event = new CycleModeEvent(mode);
    }

    private InstantAction? GetSelectModeAction(GunComponent component)
    {
        if (component.SelectedMode == component.AvailableModes) return null;

        var nextMode = GetNextMode(component);

        var action = new InstantAction()
        {
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Interface/Actions/scream.png")),
            Name = "Cycle mode",
            Keywords = new HashSet<string>()
            {
                "gun",
            },
        };

        UpdateSelectModeAction(action, nextMode);

        return action;
    }

    private sealed class CycleModeEvent : InstantActionEvent
    {
        public SelectiveFire Mode;

        public CycleModeEvent(SelectiveFire mode)
        {
            Mode = mode;
        }
    }

    private void OnCycleMode(EntityUid uid, GunComponent component, CycleModeEvent args)
    {
        SelectFire(component, args.Mode, args.Performer);
    }
}
