using System.Numerics;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Combat;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CF.Hud;

/// <summary>
/// One diamond per action the creature can hold. A diamond is lit while that action is ready.
/// </summary>
public sealed class CFActionPips : BoxContainer
{
    private int _ready = -1;
    private int _max = -1;

    public CFActionPips()
    {
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 2;
        Visible = false;
        MouseFilter = MouseFilterMode.Stop;
    }

    public void Set(CFActionPoolComponent? pool)
    {
        if (pool == null || pool.MaxActions <= 0)
        {
            Visible = false;
            _ready = -1;
            _max = -1;
            return;
        }

        var per = pool.StaminaPerAction <= 0f ? 1f : pool.StaminaPerAction;
        var ready = Math.Clamp((int) MathF.Floor(Math.Max(0f, pool.Stamina) / per), 0, pool.MaxActions);
        var seconds = (pool.RegenSeconds * per).ToString("0.#");
        var map = CFMelee.MapPenalty(pool.MapAttacks, agile: false);
        var agile = CFMelee.MapPenalty(pool.MapAttacks, agile: true);
        ToolTip = Loc.GetString("cf-hud-action-pool",
            ("current", ready),
            ("max", pool.MaxActions),
            ("seconds", seconds),
            ("map", map),
            ("agile", agile));

        if (ready == _ready && pool.MaxActions == _max && Visible)
            return;

        _ready = ready;
        _max = pool.MaxActions;
        RemoveAllChildren();
        for (var i = 0; i < pool.MaxActions; i++)
        {
            AddChild(new Label
            {
                Text = "◆",
                FontColorOverride = i < ready ? new Color(96, 176, 88) : new Color(72, 72, 76),
                MinSize = new Vector2(16, 20),
                HorizontalAlignment = HAlignment.Center,
            });
        }

        Visible = true;
    }
}
