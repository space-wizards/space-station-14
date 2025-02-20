// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.LieDown;

public abstract class SharedLieDownSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedBuckleSystem _buckleSystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LieDownComponent, LieDownDoAfterEvent>(OnLieDownDoAfterEvent);
        SubscribeLocalEvent<LieDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<LieDownComponent, StandAttemptEvent>(OnStandAttemptEvent);

        SubscribeLocalEvent<LieDownComponent, BuckledEvent>((ent, comp, _) => EntityUp(ent, comp));
        SubscribeLocalEvent<LieDownComponent, CuffedStateChangeEvent>((ent, comp, _) =>
        {
            if (!TryComp<CuffableComponent>(ent, out var cuffableComponent))
                return;

            if (!cuffableComponent.CanStillInteract)
                EntityUp(ent, comp);
        });

        CommandBinds.Builder
            .Bind(DeadSpaceKeys.LieDown, InputCmdHandler.FromDelegate(LieDownHandler, handle: false))
            .Register<SharedLieDownSystem>();
    }

    /// <summary>
    /// При нажатии клавиши - получаем только сессию пользователя. Вытаскиваем энтити и продолжаем работать.
    /// </summary>
    private void LieDownHandler(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } entity || !Exists(entity))
            return;

        if (!TryComp<LieDownComponent>(entity, out var comp))
            return;

        TryLieDown(entity, comp);
    }

    /// <summary>
    /// Создаём doAfter (полоска над персонажем) для энтити.
    /// </summary>
    private void TryLieDown(EntityUid entity, LieDownComponent comp)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, entity, comp.IsLieDown ? comp.UpDelay : comp.DownDelay, new LieDownDoAfterEvent(), entity)
        {
            BreakOnDamage = true,
            RequireCanInteract = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// По завершению doAfter уже решаем что делать с персонажем.
    /// </summary>
    private void OnLieDownDoAfterEvent(EntityUid entity, LieDownComponent comp, LieDownDoAfterEvent ev)
    {
        // Ивент вызывается даже если doAfter прерывается, проверяем вручную
        if (ev.Cancelled)
            return;

        if (comp.IsLieDown)
        {
            EntityUp(entity, comp);
        }
        else
        {
            EntityDown(entity, comp);
        }
    }

    /// <summary>
    /// Проверяем может ли entity лечь, и если да то помогаем ему с этим очищая измененные fixtures (дабы не лезли под столы, ибо это сломано)
    /// </summary>
    private void EntityDown(EntityUid entity, LieDownComponent comp)
    {
        if (comp.IsLieDown || !TryComp<StandingStateComponent>(entity, out var standingState) || !standingState.Standing)
            return;

        if (_buckleSystem.IsBuckled(entity))
            _buckleSystem.TryUnbuckle(entity, entity);

        comp.IsLieDown = true;
        comp.DrawDowned = true;
        _standing.Down(entity, dropHeldItems: false);

        if (TryComp(entity, out FixturesComponent? fixtureComponent))
        {
            foreach (var key in standingState.ChangedFixtures)
            {
                if (fixtureComponent.Fixtures.TryGetValue(key, out var fixture))
                    _physics.SetCollisionMask(entity, key, fixture, fixture.CollisionMask | (int) CollisionGroup.MidImpassable, fixtureComponent);
            }
        }
        standingState.ChangedFixtures.Clear();
        EnsureAfter(entity, comp);
    }

    /// <summary>
    /// Встаём. Собственно да.
    /// </summary>
    private void EntityUp(EntityUid entity, LieDownComponent comp)
    {
        if (!comp.IsLieDown)
            return;

        comp.IsLieDown = false;
        comp.DrawDowned = false;
        _standing.Stand(entity);
        EnsureAfter(entity, comp);
    }

    private void EnsureAfter(EntityUid entity, LieDownComponent comp)
    {
        // Напоминаем системе применить изменения скорости, ну а Dirty дабы всё засинхронилось (Почему-то данные на клиенте и сервере сильно различались, стоит проверить позже)
        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);
        Dirty(entity, comp);
    }

    private void OnRefreshMovespeed(EntityUid uid, LieDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!comp.IsLieDown)
            return;

        args.ModifySpeed(comp.WalkSpeedModifier, comp.RunSpeedModifier);
    }

    private void OnStandAttemptEvent(EntityUid ent, LieDownComponent comp, StandAttemptEvent ev)
    {
        if (comp.IsLieDown)
            ev.Cancel();
    }
}

[Serializable, NetSerializable]
public sealed partial class LieDownDoAfterEvent : SimpleDoAfterEvent;
