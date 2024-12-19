using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public abstract class SharedPaperQuantumSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperQuantumComponent, GetVerbsEvent<Verb>>(AddVerbs);
        SubscribeLocalEvent<PaperQuantumComponent, PaperQuantumSplitDoAfter>(OnSplit);
        SubscribeLocalEvent<PaperQuantumComponent, PaperQuantumDisentangleDoAfter>(OnDisentangle);
        SubscribeLocalEvent<PaperQuantumComponent, StampedEvent>(OnStamped);
    }

    // Quantum plitting functionality

    private void AddVerbs(EntityUid uid, PaperQuantumComponent component, GetVerbsEvent<Verb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString(component.DisentangleVerb),
            Act = () => TryStartDisentangle((uid, component), args.User)
        });
        if (component.Entangled is null)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString(component.SplitVerb),
                Act = () => TryStartSplit((uid, component), args.User)
            });
        }
    }

    // Quantum split verb, starts DoAfter.
    private bool TryStartSplit(Entity<PaperQuantumComponent> entity, EntityUid user)
    {
        if (entity.Comp.Entangled is not null) // papers already entangled can't be split.
            return false;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            entity.Comp.SplitDuration,
            new PaperQuantumSplitDoAfter(),
            eventTarget: entity,
            target: user,
            used: entity)
        {
            NeedHand = true,
            BreakOnDamage = true,
            DistanceThreshold = 0.6f,
            MovementThreshold = 0.01f,
            BreakOnHandChange = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return false;

        _popup.PopupPredicted(
            Loc.GetString(entity.Comp.PopupSplitSelf),
            Loc.GetString(entity.Comp.PopupSplitOther, ("user", Identity.Entity(user, EntityManager))),
            user,
            user
        );
        return true;
    }

    // Post DoAfter. Create a new entity from the prototype and "entangle" both.
    private void OnSplit(Entity<PaperQuantumComponent> entity, ref PaperQuantumSplitDoAfter args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        var otherUid = Spawn(entity.Comp.QuantumPaperProto);
        var otherComp = EntityManager.GetComponent<PaperQuantumComponent>(otherUid);

        if (TryComp(entity.Owner, out PaperComponent? paperComp))
        {
            _paper.Fill(otherUid, paperComp.Content, paperComp.StampState, paperComp.StampedBy, paperComp.EditingDisabled);
        }

        EntangleOne(entity, entity.Comp.EntangledName1, otherUid);
        EntangleOne((otherUid, otherComp), entity.Comp.EntangledName2, entity.Owner);

        _handsSystem.PickupOrDrop(args.User, otherUid);
    }

    // Entangle one of the papers with some other, setting its name and description.
    private void EntangleOne(Entity<PaperQuantumComponent> entity, string locName, EntityUid otherUid)
    {
        entity.Comp.Entangled = GetNetEntity(otherUid);
        Dirty(entity);

        if (TryComp(entity.Owner, out MetaDataComponent? metaComp))
        {
            _meta.SetEntityName(entity.Owner, Loc.GetString(locName), metaComp);
            _meta.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.EntangledDesc), metaComp);
        }
    }

    // Disentangle

    // Quantum disentangle verb, starts DoAfter.
    private bool TryStartDisentangle(Entity<PaperQuantumComponent> entity, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            entity.Comp.DisentangleDuration,
            new PaperQuantumDisentangleDoAfter(),
            eventTarget: entity,
            target: user,
            used: entity)
        {
            NeedHand = true,
            BreakOnDamage = true,
            DistanceThreshold = 0.6f,
            MovementThreshold = 0.01f,
            BreakOnHandChange = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return false;

        _popup.PopupPredicted(
            Loc.GetString(entity.Comp.PopupDisentangleSelf),
            Loc.GetString(entity.Comp.PopupDisentangleOther, ("user", Identity.Entity(user, EntityManager))),
            user,
            user
        );
        return true;
    }

    // Post DoAfter. Disentangles one or both copies.
    private void OnDisentangle(Entity<PaperQuantumComponent> entity, ref PaperQuantumDisentangleDoAfter args)
    {
        if (args.Cancelled)
            return;

        if (TryGetEntity(entity.Comp.Entangled, out var entangled))
            DisentangleOne(entangled.Value);
        DisentangleOne((entity.Owner, entity.Comp));
    }

    // Disentangle one of the papers without touching the other.
    protected virtual void DisentangleOne(Entity<PaperQuantumComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Entangled = null;
        if (TryComp(entity.Owner, out MetaDataComponent? metaComp))
        {
            _meta.SetEntityName(entity.Owner, Loc.GetString(entity.Comp.DisentangledName), metaComp);
            _meta.SetEntityDescription(entity.Owner, Loc.GetString(entity.Comp.DisentangledDesc), metaComp);
        }
        RemCompDeferred<PaperQuantumComponent>(entity);
    }

    // When one is stamped, try to transfer the stamp and place some bluespace effects with the color of the stamp.
    private void OnStamped(Entity<PaperQuantumComponent> entity, ref StampedEvent args)
    {
        if (!TryGetEntity(entity.Comp.Entangled, out var entangled))
            return;
        _paper.TryStamp(entangled.Value, args.StampInfo, args.SpriteStampState);

        if (!_net.IsServer)
            return;
        var light = Spawn(entity.Comp.BluespaceEffectProto, entangled.Value.ToCoordinates());
        _light.SetColor(light, args.StampInfo.StampedColor);
    }

    /// <summary>
    /// Check if this entity is entangled with some other.
    /// </summary>
    public bool IsEntangled(Entity<PaperQuantumComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;
        return entity.Comp.Entangled is not null;
    }

    /// <summary>
    /// As quantum entanglement involves two entities at maximum, whenever some of the entangled entities
    /// is cloned, entanglement should be transferred instead.
    /// This method tries to disentangle the source entity and entangle the target.
    /// Used for Fax copying/sending.
    /// </summary>
    public void TryTransferQuantum(Entity<PaperQuantumComponent?> source, Entity<PaperQuantumComponent?> target)
    {
        if (!Resolve(source, ref source.Comp) || !Resolve(target, ref target.Comp))
            return;
        target.Comp.Entangled = source.Comp.Entangled;
        target.Comp.TeleportWeight = (int) (source.Comp.TeleportWeight * source.Comp.WeightPenaltyCoeffOnTransfer); // teleport weight halved on each transfer.
        Dirty(target);
        if (TryGetEntity(source.Comp.Entangled, out var entangled) && TryComp(entangled, out PaperQuantumComponent? entangledComp))
        {
            entangledComp.Entangled = GetNetEntity(target.Owner);
            Dirty(entangled.Value, entangledComp);
        }
        DisentangleOne(source);
    }
}

[Serializable, NetSerializable]
public sealed partial class PaperQuantumSplitDoAfter : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class PaperQuantumDisentangleDoAfter : SimpleDoAfterEvent
{
}
