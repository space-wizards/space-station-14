// Content.Client/_Stargazer/SpiderLegs/SpiderLegsSystem.cs
using System.Numerics;
using Content.Shared._Stargazer.SpiderLegs;
using Content.Shared.Camera;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Stargazer.SpiderLegs;

/// <summary>
/// Клиентская процедурная анимация паучьих ног.
/// Каждая стопа стоит на полу, пока остаётся в своей "мёртвой зоне"; когда выходит из неё —
/// резко отрывается, переносится по воздуху вперёд по ходу движения и вбивается в пол.
/// Удар даёт след, тряску камеры и звук. Всё это существует только на клиенте.
/// </summary>
public sealed partial class SpiderLegsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;

    // Фазы шага (доли от StepTime):
    private const float LiftEnd = 0.2f;   // 0..0.2   — стопа рывком взлетает вверх
    private const float MoveStart = 0.12f; // 0.12..0.8 — перенос по воздуху
    private const float MoveEnd = 0.8f;
    private const float SlamStart = 0.78f; // 0.78..0.92 — удар сверху вниз с ускорением
    private const float SlamEnd = 0.92f;   // момент касания пола

    private const int MaxFootprints = 400;

    // Длина полностью втянутой ноги, тайлов.
    private const float TuckedLength = 0.3f;

    // На каком расстоянии за точкой хвата проверяем, что корпус ещё на месте.
    private const float HullProbe = 0.35f;

    // Небольшой запас, чтобы нога на границе радиуса хватки не дёргалась туда-сюда.
    private const float HullReleaseSlack = 0.1f;

    /// <summary>Следы всех паучьих ног. Только клиент, никаких сущностей.</summary>
    public readonly List<SpiderFootprint> Footprints = new();

    public override void Initialize()
    {
        base.Initialize();
        InitializeObstacles();
        _overlay.AddOverlay(new SpiderLegsOverlay(EntityManager, this, _xform, _sprite));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<SpiderLegsOverlay>();
        Footprints.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (frameTime <= 0f)
            return;

        UpdateFootprints(frameTime);

        var query = EntityQueryEnumerator<SpiderLegsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdateLegs(uid, comp, xform, frameTime);
        }
    }

    private void UpdateFootprints(float dt)
    {
        for (var i = Footprints.Count - 1; i >= 0; i--)
        {
            var fp = Footprints[i];
            fp.Age += dt;
            if (fp.Age >= fp.Lifetime || !Exists(fp.Parent))
                Footprints.RemoveAt(i);
            else
                Footprints[i] = fp;
        }
    }

    private void UpdateLegs(EntityUid uid, SpiderLegsComponent comp, TransformComponent xform, float dt)
    {
        var parent = xform.ParentUid;
        if (!parent.IsValid() || comp.Legs.Count == 0)
            return;

        // Всё считается в координатах родителя (грида), а не мира.
        // Тогда на летящем шаттле стопы не скользят по полу.
        var pos = xform.LocalPosition;

        // Препятствия ищем только когда родитель — грид (тогда координаты ног = координаты тайлов).
        BeginObstacleQuery(comp, xform);

        if (!comp.LegsReady || comp.StateParent != parent || comp.State.Length != comp.Legs.Count)
            Reset(comp, parent, pos, xform.LocalRotation);

        // Плавно доворачиваем раскладку ног за трансформом (Angle.Lerp идёт по кратчайшей дуге).
        comp.Heading = Angle.Lerp(comp.Heading, xform.LocalRotation, 1f - MathF.Exp(-comp.TurnSpeed * dt));
        var rot = comp.Heading;

        // Сглаженная скорость корпуса.
        var instVel = (pos - comp.LastPos) / dt;
        if (instVel.LengthSquared() > 50f * 50f) // телепорт
            instVel = Vector2.Zero;
        comp.Velocity = Vector2.Lerp(comp.Velocity, instVel, 1f - MathF.Exp(-12f * dt));
        comp.LastPos = pos;

        var speed = comp.Velocity.Length();
        var idle = speed < 0.05f;
        comp.IdleTime = idle ? comp.IdleTime + dt : 0f;

        // После задержки ноги "втягиваются": точки покоя сжимаются к корпусу.
        var settled = comp.IdleTime > comp.IdleDelay;
        var restScale = settled ? comp.IdleRetract : 1f;
        var moveDir = speed > 0.1f ? comp.Velocity / speed : Vector2.Zero;

        // "Вверх по экрану" в координатах родителя: так в 2.5D показывается высота над полом.
        var parentRot = _xform.GetWorldRotation(parent);
        var worldUp = (-_eye.CurrentEye.Rotation).RotateVec(Vector2.UnitY);
        var localUp = (-parentRot).RotateVec(worldUp);

        // Радиус, в котором ноги могут держаться за обшивку из космоса. Тот же, что у хватки
        // в геймплее (SpiderGrip): если ноги держатся за обшивку — хватка точно есть.
        var hullRange = TryComp<SpiderGripComponent>(uid, out var grip) ? grip.GripRange : comp.MaxReach;

        // Битовая маска групп, у которых сейчас есть шагающая нога.
        var steppingGroups = 0;
        for (var i = 0; i < comp.State.Length; i++)
        {
            if (comp.State[i].Stepping)
                steppingGroups |= 1 << comp.Legs[i].Group;
        }

        for (var i = 0; i < comp.Legs.Count; i++)
        {
            var def = comp.Legs[i];
            var st = comp.State[i];

            var root = pos + rot.RotateVec(def.Attach);
            var idealRaw = pos + rot.RotateVec(def.Rest * restScale);

            // Стопа ставится не в исходную точку, а вперёд по ходу движения внутри зоны.
            var target = idealRaw + moveDir * (comp.StepThreshold * comp.StepOvershoot);

            // Не дальше, чем дотягивается нога.
            var reach = target - root;
            var reachLen = reach.Length();
            var maxPlace = comp.MaxReach * 0.95f;
            if (reachLen > maxPlace)
                target = root + reach * (maxPlace / reachLen);

            // Куда ставить стопу и на что она будет опираться.
            //  - Корпус стоит на полу: стопа на свободный пол в прямой видимости
            //    (у стен — к их границе), как обычно.
            //  - Корпус висит над космосом: стопа цепляется за ближайшую кромку корпуса станции
            //    в пределах досягаемости, а если её нет — нога полностью втягивается к корпусу.
            Vector2 ideal;
            SpiderFootHold targetHold;
            var targetNormal = Vector2.Zero;
            var tuck = TuckPoint(comp, pos, root);

            if (!ObstaclesActive || Classify(pos) != SpiderCell.Space)
            {
                // Идеальную точку тоже переносим: иначе у стены мёртвая зона считалась бы
                // от точки внутри стены и нога дёргалась бы на месте.
                ideal = ResolveFoot(comp, pos, root, idealRaw);
                target = ResolveFoot(comp, pos, root, target);
                targetHold = SpiderFootHold.Floor;
            }
            else if (TryHullGrab(comp, pos, root, target, hullRange, out var hullPoint, out var hullNormal))
            {
                target = hullPoint;
                targetNormal = hullNormal;
                targetHold = SpiderFootHold.Hull;
                ideal = target;
            }
            else
            {
                target = tuck;
                targetHold = SpiderFootHold.Tucked;
                ideal = target;
            }

            if (st.Stepping)
            {
                var prev = st.StepProgress;
                st.StepProgress += dt / comp.StepTime;
                var landed = prev < SlamEnd && st.StepProgress >= SlamEnd;

                if (st.StepProgress >= 1f)
                {
                    st.Ground = target;
                    st.Hold = targetHold;
                    st.HullNormal = targetNormal;
                    st.StepProgress = -1f;
                    st.Height = 0f;
                }
                else
                {
                    // Цель пересчитывается каждый кадр, поэтому ноги реагируют на повороты.
                    st.Ground = Vector2.Lerp(st.StepFrom, target, MoveCurve(st.StepProgress));
                    st.Height = HeightCurve(st.StepProgress);
                }

                // Удар (звук, след, тряска) — только о пол. За корпус в космосе нога цепляется молча.
                if (landed && targetHold == SpiderFootHold.Floor)
                    OnFootImpact(comp, parent, st.Ground, st.Ground - root);
            }
            else if (st.Hold == SpiderFootHold.Tucked)
            {
                // Втянутая нога висит у корпуса и движется вместе с ним,
                // пока не появится за что зацепиться.
                st.Height = 0f;
                st.Ground = tuck;

                if (targetHold != SpiderFootHold.Tucked)
                {
                    st.StepFrom = st.Ground;
                    st.StepProgress = 0f;
                    steppingGroups |= 1 << def.Group;
                }
            }
            else
            {
                st.Height = 0f;
                var dist = Vector2.Distance(st.Ground, root);
                var off = Vector2.Distance(st.Ground, ideal);

                // Корпус улетел слишком далеко (телепорт, рывок): переставляем стопу без анимации.
                if (off > comp.Length * 4f)
                {
                    st.Ground = ideal;
                    st.Hold = targetHold;
                    st.HullNormal = targetNormal;
                }
                else
                {
                    // Опора потеряна:
                    //  - на полу: под стопой стена (закрылась дверь) или стена между корпусом и стопой;
                    //  - на корпусе: за кромкой, за которую держались, больше ничего нет.
                    var lost = st.Hold == SpiderFootHold.Floor
                        ? IsBlocked(st.Ground) || !HasLineOfSight(pos, st.Ground)
                        : Classify(st.Ground + st.HullNormal * HullProbe) == SpiderCell.Space
                          // Корпус кресла отошёл от обшивки дальше радиуса хватки — отпускаем,
                          // иначе нога держалась бы, а хватки (и движения) уже не было бы.
                          || Vector2.Distance(pos, st.Ground) > hullRange + HullReleaseSlack;

                    var want = dist > comp.MaxReach
                               || dist < comp.MinReach
                               || off > comp.StepThreshold                  // вышла из мёртвой зоны
                               || (settled && off > comp.IdleTolerance)      // подтянуть после паузы
                               || st.Hold != targetHold                      // корпус сошёл с пола / вернулся
                               || lost;

                    // Перерастянутая нога или потерявшая опору шагает вне очереди.
                    var emergency = dist > comp.Length * comp.MaxExtension * comp.OverstretchFactor || lost;
                    var otherGroupsStepping = (steppingGroups & ~(1 << def.Group)) != 0;

                    if (want && (emergency || !otherGroupsStepping))
                    {
                        st.StepFrom = st.Ground;
                        st.StepProgress = 0f;
                        steppingGroups |= 1 << def.Group;
                    }
                }
            }

            // Поднятая стопа рисуется выше по экрану, нога при этом сильнее выгибается.
            var foot = st.Ground + localUp * (st.Height * comp.StepHeight);
            SolveLegShape(comp, def, st, root, foot, dt);
        }
    }

    /// <summary>
    /// Строит дугу ноги с втягиванием.
    /// Длина ноги следует за расстоянием от корня до стопы (с тем же запасом, что в покое),
    /// поэтому нога всегда почти прямая: у стен и в коридорах она короче, а лишние сегменты
    /// уходят в корпус. Если укороченная дуга всё равно задевает стену — нога втягивается ещё,
    /// вплоть до прямой линии.
    /// </summary>
    private void SolveLegShape(SpiderLegsComponent comp, SpiderLegDef def, SpiderLegState st,
        Vector2 root, Vector2 foot, float dt)
    {
        // Сколько длины приходится на расстояние "корень → стопа" в покое.
        var restChord = (def.Rest - def.Attach).Length();
        var ratio = restChord > 1e-4f ? comp.Length / restChord : 1f;

        // Целевая длина — по стопе на полу (подъём при шаге добавляется отдельно).
        var chord = Vector2.Distance(st.Ground, root);
        var target = st.Hold == SpiderFootHold.Tucked && !st.Stepping
            ? MathF.Max(TuckedLength, chord * ratio)   // полностью втянута — короче MinLength
            : Math.Clamp(chord * ratio, comp.MinLength, comp.Length * comp.MaxExtension);

        var speed = target < st.CurrentLength ? comp.RetractSpeed : comp.ExtendSpeed;
        st.CurrentLength = MoveTowards(st.CurrentLength, target, speed * dt);

        var length = st.CurrentLength + st.Height * comp.StepLift;
        var straight = Vector2.Distance(foot, root);

        // Дуга задевает стену — втягиваем, пока не перестанет (бисекция до прямой линии).
        var segments = SolveAt(comp, def, st, root, foot, length);
        if (CountBlocked(st.Joints, segments) > 0 && length > straight)
        {
            var lo = straight;
            var hi = length;
            for (var i = 0; i < 8; i++)
            {
                var mid = (lo + hi) * 0.5f;
                segments = SolveAt(comp, def, st, root, foot, mid);
                if (CountBlocked(st.Joints, segments) == 0)
                    lo = mid;
                else
                    hi = mid;
            }

            segments = SolveAt(comp, def, st, root, foot, lo);
        }

        st.JointCount = segments;
    }

    /// <summary>Решает дугу заданной длины; число сегментов зависит от длины (сегменты одной длины).</summary>
    private static int SolveAt(SpiderLegsComponent comp, SpiderLegDef def, SpiderLegState st,
        Vector2 root, Vector2 foot, float length)
    {
        var segLen = comp.Length / Math.Max(1, comp.Segments);
        var segments = Math.Clamp((int) MathF.Round(length / segLen), 4, st.Joints.Length - 1);

        SpiderLegMath.Solve(root, foot, length, def.Bend,
            comp.ControlA, comp.ControlB, comp.ControlAOffset, comp.ControlBOffset, segments, st.Joints);

        return segments;
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        return current < target
            ? MathF.Min(target, current + maxDelta)
            : MathF.Max(target, current - maxDelta);
    }

    private void OnFootImpact(SpiderLegsComponent comp, EntityUid parent, Vector2 ground, Vector2 legDir)
    {
        // След: трёхлучевая звезда, повёрнутая по направлению ноги.
        if (comp.FootprintState != null && comp.FootprintLifetime > 0f)
        {
            if (Footprints.Count >= MaxFootprints)
                Footprints.RemoveAt(0);

            Footprints.Add(new SpiderFootprint
            {
                Parent = parent,
                Position = ground,
                Rotation = new Angle(MathF.Atan2(legDir.Y, legDir.X)),
                Sprite = comp.Sprite,
                State = comp.FootprintState,
                Size = comp.FootprintSize,
                Color = comp.FootprintColor,
                Lifetime = comp.FootprintLifetime,
                FadeTime = MathF.Max(0.01f, comp.FootprintFadeTime),
            });
        }

        var coords = new EntityCoordinates(parent, ground);

        if (comp.StepSound != null)
            _audio.PlayStatic(comp.StepSound, Filter.Local(), coords, false);

        // Тряска камеры у локального игрока, затухает с расстоянием.
        if (comp.ShakeStrength <= 0f || _player.LocalEntity is not { } local)
            return;

        var localXform = Transform(local);
        var impactWorld = Vector2.Transform(ground, _xform.GetWorldMatrix(parent));
        if (localXform.MapID != Transform(parent).MapID)
            return;

        var d = Vector2.Distance(_xform.GetWorldPosition(localXform), impactWorld);
        if (d >= comp.ShakeRange)
            return;

        var strength = comp.ShakeStrength * (1f - d / comp.ShakeRange);
        // Камеру "подбрасывает" вниз по экрану — ощущение тяжёлого удара о пол.
        var worldDown = (-_eye.CurrentEye.Rotation).RotateVec(-Vector2.UnitY);
        _recoil.KickCamera(local, worldDown * strength);
    }

    private static int MaxSegments(SpiderLegsComponent comp)
    {
        var segLen = comp.Length / Math.Max(1, comp.Segments);
        return (int) MathF.Ceiling((comp.Length * comp.MaxExtension + comp.StepLift) / segLen) + 1;
    }

    /// <summary>Горизонтальный перенос стопы: стоит на месте при отрыве, доезжает до удара.</summary>
    private static float MoveCurve(float p)
    {
        var u = Math.Clamp((p - MoveStart) / (MoveEnd - MoveStart), 0f, 1f);
        return u * u * (3f - 2f * u);
    }

    /// <summary>
    /// Высота стопы: резкий рывок вверх (кубическое замедление), зависание,
    /// затем падение с ускорением — стопа "вбивается" в пол.
    /// </summary>
    private static float HeightCurve(float p)
    {
        if (p < LiftEnd)
        {
            var t = 1f - p / LiftEnd;
            return 1f - t * t * t;
        }

        if (p < SlamStart)
            return 1f;

        if (p < SlamEnd)
        {
            var t = (p - SlamStart) / (SlamEnd - SlamStart);
            return 1f - t * t;
        }

        return 0f;
    }

    private void Reset(SpiderLegsComponent comp, EntityUid parent, Vector2 pos, Angle rot)
    {
        comp.State = new SpiderLegState[comp.Legs.Count];
        for (var i = 0; i < comp.Legs.Count; i++)
        {
            var def = comp.Legs[i];
            comp.State[i] = new SpiderLegState
            {
                Ground = ResolveFoot(comp, pos, pos + rot.RotateVec(def.Attach), pos + rot.RotateVec(def.Rest)),
                // Запас под удлинение ноги при подъёме во время шага.
                Joints = new Vector2[MaxSegments(comp) + 1],
                JointCount = Math.Max(1, comp.Segments),
                CurrentLength = comp.Length,
            };
        }

        comp.StateParent = parent;
        comp.LastPos = pos;
        comp.Velocity = Vector2.Zero;
        comp.Heading = rot;
        comp.IdleTime = 0f;
        comp.LegsReady = true;
    }
}

public struct SpiderFootprint
{
    public EntityUid Parent;
    public Vector2 Position;
    public Angle Rotation;
    public string Sprite;
    public string State;
    public float Size;
    public Color Color;
    public float Lifetime;
    public float FadeTime;
    public float Age;
}
