using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.RatKing;

namespace Content.Server.RatKing;

/// <inheritdoc/>
public sealed class RatKingDomainSystem : SharedRatKingDomainSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatKingDomainComponent, RatKingDomainActionEvent>(OnDomain);
    }

    /// <summary>
    /// uses hunger to release a specific amount of ammonia into the air. This heals the rat king
    /// and his servants through a specific metabolism.
    /// </summary>
    private void OnDomain(EntityUid uid, RatKingDomainComponent component, RatKingDomainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<HungerComponent>(uid, out var hunger))
            return;

        //make sure the hunger doesn't go into the negatives
        if (hunger.CurrentHunger < component.HungerPerDomainUse)
        {
            _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
            return;
        }

        args.Handled = true;
        _hunger.ModifyHunger(uid, -component.HungerPerDomainUse, hunger);

        _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), uid);
        var tileMix = _atmos.GetTileMixture(uid, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, component.MolesAmmoniaPerDomain);
    }
}
