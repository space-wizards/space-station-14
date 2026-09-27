using Content.Shared.CosmicCult.Components;
using Content.Shared.Dataset;
using Content.Shared.Effects;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult;

/// <summary>
/// System for Cosmic Cult's Malign Rifts. Handles cultist interact/absorb and lambda-particle expunging.
/// </summary>
public abstract partial class CosmicRiftSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedPopupSystem _popup = default!;


    [SubscribeLocalEvent]
    private void OnCollide(Entity<CosmicRiftComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.HitTimer != null)
            return;

        if (!HasComp<CosmicLambaParticleComponent>(args.OtherEntity) || !TryComp<ProjectileComponent>(args.OtherEntity, out var projectile) || !HasComp<CosmicLambdaDeviceComponent>(projectile.Shooter))
            return;

        _color.RaiseEffect(Color.FromHex("#9F18FF"), new List<EntityUid>() { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        var random = SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(ent));
        var text = GetText(ent.Comp.PopUpDataset, random);

        if (random.Prob(ent.Comp.TextChance) && !string.IsNullOrWhiteSpace(text))
            _popup.PopupEntity(Loc.GetString(text), ent, PopupType.Medium);

        ent.Comp.CurrentHits++;
        ent.Comp.HitTimer = Timing.CurTime + TimeSpan.FromSeconds(0.5f);

        if (ent.Comp.CurrentHits < ent.Comp.MaxHits)
            return;

        if (_net.IsServer)
        {
            var vfx = Spawn(ent.Comp.ExpungeVfx, Transform(ent).Coordinates);
            _audio.PlayPvs(ent.Comp.ExpungeSound, vfx);
        }

        PredictedQueueDel(ent);
    }

    /// <summary>
    /// Used to store the Grid into the Rift Component so that it can be referenced during ComponentShutdown.
    /// We do this because our on-server ComponentShutdown may not be able to locate the TransformComponent at the time of shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CosmicRiftComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).GridUid is not { } grid)
            return;

        ent.Comp.GridUid = grid;
    }

    private string? GetText(ProtoId<LocalizedDatasetPrototype> dialogue, IRobustRandom random)
    {
        if (!ProtoMan.Resolve(dialogue, out var proto))
            return null;

        return random.Pick(proto.Values);
    }
}
