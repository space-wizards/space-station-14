using Content.Server.Power.Components;
using Content.Shared.BluespaceHarvester;
using Content.Shared.Emag.Components;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.BluespaceHarvester;

public sealed class BluespaceHarvesterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public List<BluespaceHarvesterTap> Taps = new List<BluespaceHarvesterTap>()
    {
        new BluespaceHarvesterTap() { Level = 0, Visual = BluespaceHarvesterVisuals.Tap0 },
        new BluespaceHarvesterTap() { Level = 1, Visual = BluespaceHarvesterVisuals.Tap1 },
        new BluespaceHarvesterTap() { Level = 5, Visual = BluespaceHarvesterVisuals.Tap2 },
        new BluespaceHarvesterTap() { Level = 10, Visual = BluespaceHarvesterVisuals.Tap3 },
        new BluespaceHarvesterTap() { Level = 15, Visual = BluespaceHarvesterVisuals.Tap4 },
        new BluespaceHarvesterTap() { Level = 20, Visual = BluespaceHarvesterVisuals.Tap5 },
    };

    private float _updateTimer;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer < 1)
            return;

        _updateTimer--;

        var query = EntityQueryEnumerator<BluespaceHarvesterComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var harvester, out var reciver))
        {
            if (!reciver.Powered)
            {
                if (harvester.CurrentLevel != 0)
                    harvester.CurrentLevel--;
            }
            else
            {
                if (harvester.CurrentLevel < harvester.TargetLevel)
                    harvester.CurrentLevel++;

                harvester.Points += GetPointGeneration(uid, harvester);
            }

            if (harvester.CurrentLevel > harvester.TargetLevel)
                harvester.CurrentLevel--;

            reciver.Load = GetUsagePower(harvester.CurrentLevel);

            harvester.DangerPoints += GetDangerPointGeneration(uid, harvester);

            if (harvester.DangerPoints < 0)
                harvester.DangerPoints = 0;

            UpdateAppearance(uid, harvester);
            UpdateUI(uid, harvester);
        }
    }

    private void UpdateAppearance(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        if (HasComp<EmaggedComponent>(uid))
        {
            _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int)harvester.RedspaceTap);
            return;
        }

        var level = harvester.CurrentLevel;
        var visuals = Taps.FindAll((tap) => tap.Level <= level);

        if (visuals.Count == 0)
            return;

        var max = visuals.MaxBy((tap) => tap.Level);
        if (max == null)
            return;

        _appearance.SetData(uid, BluespaceHarvesterVisualLayers.Base, (int)max.Visual);
    }

    private void UpdateUI(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return;

        var powerUsage = GetUsagePower(harvester.CurrentLevel);
        var powerUsageNext = GetUsagePower(harvester.CurrentLevel + 1);

        _ui.TrySetUiState(uid, BluespaceHarvesterUiKey.Key, new BluespaceHarvesterBoundUserInterfaceState(
            harvester.TargetLevel,
            harvester.CurrentLevel,
            harvester.MaxLevel,
            powerUsage,
            powerUsageNext
        ));
    }

    public uint GetUsagePower(int level)
    {
        return level switch
        {
            0 => 500,
            1 => 1_000,
            2 => 5_000,
            3 => 50_000,
            4 => 100_000,
            5 => 500_000,
            6 => 1_000_000,
            7 => 2_000_000,
            8 => 5_000_000,
            9 => 10_000_000,
            10 => 20_000_000,
            11 => 50_000_000,
            12 => 100_000_000,
            13 => 200_000_000,
            14 => 400_000_000,
            15 => 800_000_000,
            16 => 1_000_000_000,
            17 => 2_000_000_000,
            //18 => 5_000_000_000,
            //19 => 10_000_000_000,
            //20 => 20_000_000_000,
            _ => 0,
        };
    }

    public int GetPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return harvester.CurrentLevel * 4 * (HasComp<EmagComponent>(uid) ? 2 : 1);
    }

    public int GetDangerPointGeneration(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        var stable = GetStableLevel(uid, harvester);
        if (harvester.CurrentLevel <= stable)
            return -4;

        return stable / (harvester.MaxLevel - stable) * 400;
    }

    public int GetStableLevel(EntityUid uid, BluespaceHarvesterComponent? harvester = null)
    {
        if (!Resolve(uid, ref harvester))
            return 0;

        return HasComp<EmaggedComponent>(uid) ? harvester.EmaggedStableLevel : harvester.StableLevel;
    }
}
