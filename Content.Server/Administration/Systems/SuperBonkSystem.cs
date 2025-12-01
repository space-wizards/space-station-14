using Content.Server.Administration.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Clumsy;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Systems;

public sealed class SuperBonkSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ClumsySystem _clumsySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuperBonkComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SuperBonkComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SuperBonkComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<SuperBonkComponent> ent, ref ComponentInit args)
    {
        var (_, component) = ent;

        component.NextBonk = _timing.CurTime + component.BonkCooldown;
    }

    private void OnMobStateChanged(Entity<SuperBonkComponent> ent, ref MobStateChangedEvent args)
    {
        var (uid, component) = ent;

        if (component.StopWhenDead && args.NewMobState == MobState.Dead)
            RemCompDeferred<SuperBonkComponent>(uid);
    }

    private void OnShutdown(Entity<SuperBonkComponent> ent, ref ComponentShutdown args)
    {
        var (uid, component) = ent;

        if (component.RemoveClumsy)
            RemComp<ClumsyComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var comps = EntityQueryEnumerator<SuperBonkComponent>();

        while (comps.MoveNext(out var uid, out var comp))
        {
            if (comp.NextBonk > _timing.CurTime)
                continue;

            if (!TryBonk(uid, comp.Tables.Current) || !comp.Tables.MoveNext())
            {
                RemComp<SuperBonkComponent>(uid);
                continue;
            }

            comp.NextBonk += comp.BonkCooldown;
        }
    }

    public void StartSuperBonk(EntityUid target, bool stopWhenDead = false)
    {
        //The other check in the code to stop when the target dies does not work if the target is already dead.
        if (stopWhenDead && TryComp<MobStateComponent>(target, out var mobState) && mobState.CurrentState == MobState.Dead)
            return;


        if (EnsureComp<SuperBonkComponent>(target, out var component))
            return;

        var tables = EntityQueryEnumerator<BonkableComponent>();
        var bonks = new List<EntityUid>();
        // This is done so we don't crash if something like a new table is spawned.
        while (tables.MoveNext(out var uid, out var comp))
        {
            bonks.Add(uid);
        }

        component.Tables = bonks.GetEnumerator();
        component.RemoveClumsy = !EnsureComp<ClumsyComponent>(target, out _);
        component.StopWhenDead = stopWhenDead;
    }

    private bool TryBonk(EntityUid uid, EntityUid tableUid)
    {
        if (!TryComp<ClumsyComponent>(uid, out var clumsyComp))
            return false;

        // It would be very weird for something without a transform component to have a bonk component
        // but just in case because I don't want to crash the server.
        if (HasComp<TransformComponent>(tableUid))
        {
            _transformSystem.SetCoordinates(uid, Transform(tableUid).Coordinates);

            _clumsySystem.HitHeadClumsy((uid, clumsyComp), tableUid);

            _audioSystem.PlayPvs(clumsyComp.TableBonkSound, tableUid);
        }

        return true;
    }
}
