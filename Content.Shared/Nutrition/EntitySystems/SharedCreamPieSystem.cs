using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Fluids;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract class SharedCreamPieSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreamPieComponent, ThrowDoHitEvent>(OnCreamPieHit);
        SubscribeLocalEvent<CreamPieComponent, LandEvent>(OnCreamPieLand);
        SubscribeLocalEvent<CreamPiedComponent, ThrowHitByEvent>(OnCreamPiedHitBy);
        SubscribeLocalEvent<CreamPieComponent, SliceFoodEvent>(OnSlice);
        SubscribeLocalEvent<CreamPiedComponent, RejuvenateEvent>(OnRejuvenate);
    }

    /// <summary>
    /// SPLAT!
    /// </summary>
    public void SplatCreamPie(Entity<CreamPieComponent> creamPie)
    {
        // Already splatted! Do nothing.
        if (creamPie.Comp.Splatted)
            return;

        // The pie will be queued for deletion but there may be multiple collisions in the same tick, so we prevent it from splatting more than once.
        creamPie.Comp.Splatted = true;
        Dirty(creamPie);

        // The entity is being deleted, so play the sound at its position rather than parenting.
        if (_net.IsServer) // we don't have a user to pass in TODO: make the popup API sane and remove this guard
        {
            var coordinates = Transform(creamPie).Coordinates;
            _audio.PlayPvs(creamPie.Comp.Sound, coordinates);
        }

        if (TryComp<EdibleComponent>(creamPie, out var edibleComp))
        {
            if (_solutions.TryGetSolution(creamPie.Owner, edibleComp.Solution, out _, out var solution))
                _puddle.TrySpillAt(creamPie.Owner, solution, out _, false);

            _ingestion.SpawnTrash((creamPie.Owner, edibleComp));
        }

        ActivatePayload(creamPie);
        PredictedQueueDel(creamPie);
    }

    /// <summary>
    /// Drop any item hidden in the cream pie and trigger it.
    /// </summary>
    public void ActivatePayload(EntityUid uid)
    {
        // Keep this server side for now since we don't have a user we can pass in for prediction purposes.
        // Ideally the popup and audio API will be reworked so that is not needed anymore.
        if (_net.IsClient)
            return;

        if (_itemSlots.TryGetSlot(uid, CreamPieComponent.PayloadSlotName, out var itemSlot)
            && _itemSlots.TryEject(uid, itemSlot, user: null, out var item)
            && TryComp<TimerTriggerComponent>(item.Value, out var timerTrigger))
            _trigger.ActivateTimerTrigger((item.Value, timerTrigger));
    }

    /// <summary>
    /// Sets the creampied status of an entity.
    /// This toggles the visuals for the pie in their face.
    /// </summary>
    public void SetCreamPied(Entity<CreamPiedComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (value == ent.Comp.CreamPied)
            return;

        ent.Comp.CreamPied = value;
        Dirty(ent);

        _appearance.SetData(ent.Owner, CreamPiedVisuals.Creamed, value);
    }

    private void OnCreamPieLand(Entity<CreamPieComponent> ent, ref LandEvent args)
    {
        SplatCreamPie(ent);
    }

    private void OnCreamPieHit(Entity<CreamPieComponent> ent, ref ThrowDoHitEvent args)
    {
        SplatCreamPie(ent);
    }

    private void OnCreamPiedHitBy(Entity<CreamPiedComponent> creamPied, ref ThrowHitByEvent args)
    {
        if (creamPied.Comp.CreamPied || !Exists(args.Thrown) || !TryComp<CreamPieComponent>(args.Thrown, out var creamPie))
            return;

        // TODO: Check if they even have a head that can be hit.
        SetCreamPied(creamPied.AsNullable(), true);
        _stunSystem.TryUpdateParalyzeDuration(creamPied.Owner, creamPie.ParalyzeTime);

        // Throwing is not predicted, so the thrower is not equal to the client predicting the collision, so we cannot pass in a user.
        // TODO: Make the popup API sane.
        if (_net.IsClient)
            return;

        // Shown only to the player that was hit.
        _popup.PopupEntity(
            Loc.GetString(
                "cream-pied-component-on-hit-by-message",
                ("thrown", args.Thrown)),
            creamPied.Owner, creamPied.Owner);

        var otherPlayers = Filter.PvsExcept(creamPied.Owner);

        // Show to everyone else.
        _popup.PopupEntity(
            Loc.GetString(
                "cream-pied-component-on-hit-by-message-others",
                ("owner", Identity.Entity(creamPied.Owner, EntityManager)),
                ("thrown", args.Thrown)),
            creamPied.Owner, otherPlayers, false);
    }

    private void OnRejuvenate(Entity<CreamPiedComponent> ent, ref RejuvenateEvent args)
    {
        SetCreamPied(ent.AsNullable(), false);
    }

    // TODO
    // A regression occured here. Previously creampies would activate their hidden payload if you tried to eat them.
    // However, the refactor to IngestionSystem caused the event to not be reached,
    // because eating is blocked if an item is inside the food.

    private void OnSlice(Entity<CreamPieComponent> ent, ref SliceFoodEvent args)
    {
        ActivatePayload(ent);
    }
}
