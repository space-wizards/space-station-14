using System.Numerics;
using Content.Server._Goobstation.SpaceWhale;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Goobstation.SpaceWhale.SpaceWhaleSegment;

/// <summary>
/// Модель тела:
///   * Голова движется AI'ем как обычная физическая сущность.
///   * Сегмент i следует за сегментом i-1 (нулевой — за головой). Связь — не
///     по path головы, а по реальной цепи: target = prev.pos + alongDir × spacing.
///   * Движение сегмента — через SetLinearVelocity, физика честно сталкивается
///     со стенами. SetWorldPosition используется только при первом спавне и
///     при аварийном восстановлении (другой грид / долгий разрыв > 8×spacing).
///   * Сегменты одного кита не сталкиваются между собой и с головой
///     (PreventCollideEvent), иначе цепь залипает на самой себе.
///   * Поверх follow-движения добавляется боковая волна с фазовым сдвигом
///     по индексу — бегущая волна вдоль тела. Амплитуда растёт к хвосту,
///     модулируется скоростью головы, гасится у "толкающегося" сегмента.
///   * Если segment[0] оторвался от головы — HeadSpeedMultiplier падает,
///     голова тормозит/встаёт. Эффект распространяется по цепи естественно.
/// </summary>
public sealed partial class TailedEntitySystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const float Epsilon = 1e-4f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TailedEntityComponent, ComponentShutdown>(OnTailedShutdown);
        SubscribeLocalEvent<SpaceWhaleSegmentComponent, PreventCollideEvent>(OnSegmentPreventCollide);
#pragma warning disable CS0618
        SubscribeLocalEvent<SpaceWhaleSegmentComponent, DamageChangedEvent>(OnSegmentDamageChanged);
#pragma warning restore CS0618
    }

    private void OnTailedShutdown(EntityUid uid, TailedEntityComponent comp, ComponentShutdown args)
    {
        foreach (var seg in comp.TailSegments)
        {
            if (Exists(seg))
                QueueDel(seg);
        }
        comp.TailSegments.Clear();
        comp.SegmentStates.Clear();
        comp.HeadSpeedMultiplier = 1f;
    }

    /// <summary>
    /// Сегмент кита не сталкивается с головой и с другими сегментами того же кита.
    /// </summary>
    private void OnSegmentPreventCollide(
        Entity<SpaceWhaleSegmentComponent> ent,
        ref PreventCollideEvent args)
    {
        if (ent.Comp.Whale is not { } whale)
            return;

        if (args.OtherEntity == whale)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<SpaceWhaleSegmentComponent>(args.OtherEntity, out var other)
            && other.Whale == whale)
        {
            args.Cancelled = true;
        }
    }

#pragma warning disable CS0618
    private void OnSegmentDamageChanged(Entity<SpaceWhaleSegmentComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null || args.DamageDelta.Empty)
            return;

        if (ent.Comp.Whale is not { } whale || Deleted(whale))
            return;

        _damageable.TryChangeDamage(
            whale,
            args.DamageDelta,
            interruptsDoAfters: args.InterruptsDoAfters,
            origin: args.Origin,
            ignoreGlobalModifiers: true);

        _damageable.ClearAllDamage(ent.Owner);
    }
#pragma warning restore CS0618

    public override void Update(float frameTime)
    {
        var time = (float)_timing.CurTime.TotalSeconds;
        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.MapID == MapId.Nullspace || xform.MapUid == null)
                continue;

            var headPos = _transform.GetWorldPosition(xform);
            var headRot = _transform.GetWorldRotation(xform);

            EnsureSegments(uid, comp, xform, headPos, headRot);
            UpdateChain(uid, comp, headPos, headRot, time, frameTime);
        }
    }

    private void EnsureSegments(
        EntityUid whale,
        TailedEntityComponent comp,
        TransformComponent xform,
        Vector2 headPos,
        Angle headRot)
    {
        var mapUid = xform.MapUid;
        if (mapUid == null)
            return;

        // Срезаем лишние.
        while (comp.TailSegments.Count > comp.Amount)
        {
            var last = comp.TailSegments[^1];
            if (Exists(last))
                QueueDel(last);
            comp.TailSegments.RemoveAt(comp.TailSegments.Count - 1);
        }

        EnsureStateCount(comp);

        var prevPos = headPos;
        var forward = headRot.ToWorldVec();

        for (var i = 0; i < comp.Amount; i++)
        {
            var seg = i < comp.TailSegments.Count ? comp.TailSegments[i] : EntityUid.Invalid;
            var alive = Exists(seg) && Transform(seg).MapID == xform.MapID;

            if (!alive)
            {
                if (Exists(seg))
                    QueueDel(seg);

                var spawnPos = prevPos - forward * comp.Spacing;
                seg = Spawn(comp.Prototype, new EntityCoordinates(mapUid.Value, spawnPos));

                if (i < comp.TailSegments.Count)
                    comp.TailSegments[i] = seg;
                else
                    comp.TailSegments.Add(seg);

                // Свежий сегмент смотрит на prev — иначе цепь стартует с
                // неопределённой ориентации и в первый тик "выгибается".
                _transform.SetWorldRotation(seg, (prevPos - spawnPos).ToWorldAngle());

                var state = comp.SegmentStates[i];
                state.LastPosition = spawnPos;
                state.HasLastPosition = true;
                state.StuckTime = 0f;
                state.IsPushing = false;
            }

            var segComp = EnsureComp<SpaceWhaleSegmentComponent>(seg);
            segComp.Whale = whale;
            segComp.Index = i;

            if (Exists(seg))
                prevPos = _transform.GetWorldPosition(Transform(seg));
        }
    }

    private void EnsureStateCount(TailedEntityComponent comp)
    {
        while (comp.SegmentStates.Count < comp.Amount)
            comp.SegmentStates.Add(new TailSegmentRuntimeState());
        if (comp.SegmentStates.Count > comp.Amount)
            comp.SegmentStates.RemoveRange(comp.Amount, comp.SegmentStates.Count - comp.Amount);
    }

    private void UpdateChain(
        EntityUid whale,
        TailedEntityComponent comp,
        Vector2 headPos,
        Angle headRot,
        float time,
        float frameTime)
    {
        var prevPos = headPos;
        var maxStretchRatio = 0f;
        // prevForward — куда "смотрит" предшествующее звено. Для головы это её
        // мировое направление. Сегмент должен сидеть СЗАДИ prev по prevForward
        // (а не "там, где он сейчас оказался") — иначе цепь рассыпается веером
        // когда голова резко тормозит, а хвост напирает.
        var prevForward = headRot.ToWorldVec();

        for (var i = 0; i < comp.TailSegments.Count; i++)
        {
            var seg = comp.TailSegments[i];
            if (!Exists(seg))
                continue;
            var segXform = Transform(seg);

            var state = comp.SegmentStates[i];
            var segPos = _transform.GetWorldPosition(segXform);
            var distToPrev = (segPos - prevPos).Length();

            // Максимум растяжения по всей цепи — единый сигнал для тормоза головы.
            var stretch = distToPrev / MathF.Max(comp.Spacing, Epsilon);
            if (stretch > maxStretchRatio)
                maxStretchRatio = stretch;

            // Базовая ось: "за prev" по направлению, куда prev смотрит.
            var backDir = -prevForward;

            // Угол изгиба звена относительно prev. Амплитуда:
            //   * bell-shape вдоль тела: sin(π·t), t = (i+1)/(N+1) — голова и
            //     кончик жёстче, максимум — в середине (накопительная S-форма).
            //   * гасится при сжатии (поджат к prev — изгиба нет),
            //   * почти выключается при упоре.
            var indexNorm = (i + 1) / (float)(comp.Amount + 1);
            // sqrt(sin) — более плоский bell-shape. Фронт тела не "копьё":
            // на i=3 из 30 amplitude уже ~50% от max, а не 30%.
            var ampShape = MathF.Sqrt(MathF.Sin(MathF.PI * indexNorm));
            var compression = MathF.Min(1f, distToPrev / MathF.Max(comp.Spacing, Epsilon));
            var pushSuppress = state.IsPushing ? 0.15f : 1f;
            var phase = MathF.Sin(time * comp.WaveFrequency + (i + 1) * comp.WavePhaseStep);
            // Дыхание: глобальная медленная модуляция амплитуды ±15%, цикл ~6с.
            var breathing = 1f + 0.15f * MathF.Sin(time * 1.0f);
            var bendAngle = phase * comp.BendAmplitude * ampShape * compression * pushSuppress * breathing;

            // Подёргивание кончика хвоста: bell-shape делает последние 4 сегмента
            // жёсткими, а у живой змеи кончик — самая подвижная часть. Компенсируем.
            if (i >= comp.Amount - 4)
            {
                var tipPhase = MathF.Sin(time * 3.5f + (i + 1) * 0.8f);
                bendAngle += tipPhase * 0.04f * compression * pushSuppress;
            }

            // Желаемая позиция: spacing от prev, но direction повёрнут на bendAngle.
            // Target всегда на фиксированном радиусе → никаких скачков позиции,
            // только плавное изменение угла.
            var cosB = MathF.Cos(bendAngle);
            var sinB = MathF.Sin(bendAngle);
            var bentDir = new Vector2(
                backDir.X * cosB - backDir.Y * sinB,
                backDir.X * sinB + backDir.Y * cosB);
            var target = prevPos + bentDir * comp.Spacing;

            // Скорость к target за один кадр, с потолком.
            var delta = target - segPos;
            var deltaLen = delta.Length();
            Vector2 velocity;
            if (deltaLen > Epsilon)
            {
                var speed = MathF.Min(deltaLen / MathF.Max(frameTime, 1e-3f), comp.MaxSegmentSpeed);
                velocity = delta / deltaLen * speed;
            }
            else
            {
                velocity = Vector2.Zero;
            }

            if (TryComp<PhysicsComponent>(seg, out var segPhys))
                _physics.SetLinearVelocity(seg, velocity, body: segPhys);

            // Плавный поворот в сторону prev (сегмент "смотрит вперёд" к голове).
            var faceVec = prevPos - segPos;
            var targetAngle = faceVec.LengthSquared() > Epsilon
                ? faceVec.ToWorldAngle()
                : headRot;
            var curRot = _transform.GetWorldRotation(segXform);
            var rotLerp = MathF.Min(1f, frameTime * comp.RotationSmooth);
            var newRot = Angle.Lerp(curRot, targetAngle, rotLerp);
            _transform.SetWorldRotation(seg, newRot);

            // IsPushing: расстояние до prev заметно больше spacing → prev уехал,
            // а нас держит препятствие.
            var pushDist = comp.Spacing * comp.PushDistanceFactor;
            state.IsPushing = distToPrev > pushDist;

            // Аварийный телепорт: совсем оторвался и долго не приближается.
            var movedSince = state.HasLastPosition ? (segPos - state.LastPosition).Length() : float.MaxValue;
            if (deltaLen > comp.Spacing * 2f && movedSince < 0.03f)
                state.StuckTime += frameTime;
            else
                state.StuckTime = 0f;

            if (distToPrev > comp.Spacing * 5f && state.StuckTime > comp.StuckEmergencyDelay)
            {
                var emergencyPos = prevPos + backDir * comp.Spacing * 0.5f;
                _transform.SetWorldPosition(seg, emergencyPos);
                if (segPhys != null)
                    _physics.SetLinearVelocity(seg, Vector2.Zero, body: segPhys);
                state.StuckTime = 0f;
                state.LastPosition = emergencyPos;
                segPos = emergencyPos;
            }
            else
            {
                state.LastPosition = segPos;
            }
            state.HasLastPosition = true;

            if (TryComp<SpaceWhaleSegmentComponent>(seg, out var segComp))
                segComp.IsPushing = state.IsPushing;

            prevPos = segPos;
            // Для следующего звена forward этого сегмента = направление, куда
            // он смотрит = от себя к prev (только что обновлённый rotation).
            prevForward = newRot.ToWorldVec();
        }

        // Голова тормозит/встаёт по максимальному растяжению ЛЮБОГО звена цепи,
        // а не только head→segment[0]. Если застрял segment[15], голова всё равно
        // увидит сигнал и не уедет.
        var slow = MathF.Max(1f, comp.SlowStartFactor);
        var stop = MathF.Max(slow + 0.01f, comp.SlowStopFactor);
        if (maxStretchRatio <= slow)
            comp.HeadSpeedMultiplier = 1f;
        else if (maxStretchRatio >= stop)
            comp.HeadSpeedMultiplier = 0f;
        else
            comp.HeadSpeedMultiplier = 1f - (maxStretchRatio - slow) / (stop - slow);

        // WhaleBrain тикает раз в ~0.3 сек. Между его тиками сами поддерживаем
        // velocity головы — это убирает physics-drag ("родные рывки") И
        // добавляет змеистую боковую вилку. direction берём из rotation головы
        // (она и есть "истинное" направление к цели, не зависит от прошлого
        // wiggle), wiggle добавляется перпендикулярно. Multiplier гасит всё —
        // в стене кит не дёргается.
        if (TryComp<PhysicsComponent>(whale, out var whaleBody))
        {
            var v = whaleBody.LinearVelocity;
            if (v.Length() > 0.01f)
            {
                var baseSpeed = TryComp<MovementSpeedModifierComponent>(whale, out var mod)
                    ? mod.CurrentSprintSpeed
                    : 5f;
                var headDir = headRot.ToWorldVec();
                var sideways = new Vector2(-headDir.Y, headDir.X);
                var wiggle = sideways
                             * MathF.Sin(time * comp.HeadWiggleFrequency)
                             * comp.HeadWiggleAmplitude;
                // Минимум 8% базовой скорости. Голова всегда давит в препятствие —
                // StartCollideEvent продолжает приходить, DamageOnCollide грызёт
                // мебель/стены. Полного нуля нет — иначе кит застрянет навечно.
                var mult = Math.Clamp(comp.HeadSpeedMultiplier, 0.08f, 1f);
                _physics.SetLinearVelocity(
                    whale,
                    (headDir * baseSpeed + wiggle) * mult,
                    body: whaleBody);
            }
        }
    }
}
