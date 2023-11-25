// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Light.Components;
using Content.Shared.Ghost;
using Content.Shared.Revenant.Components;
using Content.Shared.SS220.DarkReaper;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SS220.DarkReaper;

public sealed class DarkReaperSystem : SharedDarkReaperSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;

    private static readonly Color ReaperGhostColor = Color.FromHex("#bbbbff88");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperComponent, AppearanceChangeEvent>(OnAppearanceChange, after: new[] { typeof(GenericVisualizerSystem) });
        SubscribeAllEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        var query = EntityQueryEnumerator<DarkReaperComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (!_appearance.TryGetData(uid, DarkReaperVisual.PhysicalForm, out var data))
                return;

            bool hasGlare = false;
            if (_appearance.TryGetData(uid, DarkReaperVisual.StunEffect, out var glareData))
            {
                if (glareData is bool)
                    hasGlare = (bool) glareData;
            }

            bool ghostCooldown = false;
            if (_appearance.TryGetData(uid, DarkReaperVisual.GhostCooldown, out var ghostCooldownData))
            {
                if (ghostCooldownData is bool)
                    ghostCooldown = (bool) ghostCooldownData;
            }

            if (data is bool isPhysical)
                UpdateAppearance(uid, comp, sprite, isPhysical, hasGlare, ghostCooldown);
        }
    }


    private void UpdateAppearance(EntityUid uid, DarkReaperComponent comp, SpriteComponent sprite, bool isPhysical, bool hasGlare, bool ghostCooldown)
    {
        var controlled = _playerManager.LocalSession?.AttachedEntity;
        var isOwn = controlled == uid;
        var canSeeOthers = controlled.HasValue &&
                          (HasComp<GhostComponent>(controlled) ||
                           HasComp<DarkReaperComponent>(controlled) ||
                           HasComp<RevenantComponent>(controlled));
        var canSeeGhosted = isOwn || canSeeOthers;

        if (TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
        {
            if (hasGlare)
                lightBehaviour.StartLightBehaviour(comp.LightBehaviorFlicker);
            else
                lightBehaviour.StopLightBehaviour();
        }

        if (sprite.LayerMapTryGet(DarkReaperVisual.Stage, out var layerIndex))
        {
            sprite.LayerSetVisible(layerIndex, (canSeeGhosted || isPhysical) && !ghostCooldown);
            sprite.LayerSetColor(layerIndex, (canSeeGhosted && !isPhysical) ? ReaperGhostColor : Color.White);
        }

        _pointLight.SetEnabled(uid, hasGlare || ghostCooldown);
    }

    private void OnAppearanceChange(EntityUid uid, DarkReaperComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(DarkReaperVisual.PhysicalForm, out var data))
            return;

        bool hasGlare = false;
        if (args.AppearanceData.TryGetValue(DarkReaperVisual.StunEffect, out var glareData))
        {
            if (glareData is bool)
                hasGlare = (bool) glareData;
        }

        bool ghostCooldown = false;
        if (args.AppearanceData.TryGetValue(DarkReaperVisual.GhostCooldown, out var ghostCooldownData))
        {
            if (ghostCooldownData is bool)
                ghostCooldown = (bool) ghostCooldownData;
        }

        if (data is bool isPhysical)
            UpdateAppearance(uid, component, args.Sprite, isPhysical, hasGlare, ghostCooldown);
    }
}
