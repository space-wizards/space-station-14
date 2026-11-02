// Content.Client/_Stargazer/SpiderLegs/SpiderLegsOverlay.cs
using System.Numerics;
using Content.Shared._Stargazer.SpiderLegs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client._Stargazer.SpiderLegs;

public sealed class SpiderLegsOverlay : Overlay
{
    private readonly IEntityManager _ent;
    private readonly SpiderLegsSystem _legs;
    private readonly SharedTransformSystem _xform;
    private readonly SpriteSystem _sprite;
    private readonly Dictionary<(string, string), Texture> _cache = new();

    // Ноги рисуются под сущностями, поэтому кресло и седок оказываются поверх них.
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public SpiderLegsOverlay(IEntityManager ent, SpiderLegsSystem legs, SharedTransformSystem xform, SpriteSystem sprite)
    {
        _ent = ent;
        _legs = legs;
        _xform = xform;
        _sprite = sprite;
    }

    private Texture GetTexture(string rsi, string state)
    {
        if (_cache.TryGetValue((rsi, state), out var tex))
            return tex;

        tex = _sprite.Frame0(new SpriteSpecifier.Rsi(new ResPath(rsi), state));
        _cache[(rsi, state)] = tex;
        return tex;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        DrawFootprints(handle, args.MapId);

        var query = _ent.EntityQueryEnumerator<SpiderLegsComponent, TransformComponent>();
        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (!comp.LegsReady || xform.MapID != args.MapId || !xform.ParentUid.IsValid())
                continue;

            // Ноги хранятся в координатах родителя, переводим их в мировые.
            var toWorld = _xform.GetWorldMatrix(xform.ParentUid);

            var segTex = GetTexture(comp.Sprite, comp.SegmentState);
            var jointTex = comp.JointState != null ? GetTexture(comp.Sprite, comp.JointState) : null;
            var footTex = comp.FootState != null ? GetTexture(comp.Sprite, comp.FootState) : null;
            var shadowTex = comp.ShadowState != null ? GetTexture(comp.Sprite, comp.ShadowState) : null;

            // Тени поднятых стоп — под ногами.
            if (shadowTex != null)
            {
                foreach (var st in comp.State)
                {
                    if (st.Height <= 0.01f)
                        continue;

                    var ground = Vector2.Transform(st.Ground, toWorld);
                    var size = comp.FootSize * (0.8f + 0.5f * st.Height);
                    var alpha = 0.55f * (1f - 0.45f * st.Height);
                    handle.DrawTextureRect(shadowTex, Box2.CenteredAround(ground, new Vector2(size, size)),
                        Color.White.WithAlpha(alpha));
                }
            }

            foreach (var st in comp.State)
            {
                var joints = st.Joints;
                var n = Math.Min(st.JointCount, joints.Length - 1);
                if (n < 1)
                    continue;

                // Сегменты при втягивании уходят в корпус, поэтому толщину считаем от стопы:
                // кончик ноги всегда тонкий, а у втянутой ноги нет самых толстых сегментов.
                var total = Math.Max(1, comp.Segments);

                for (var i = 0; i < n; i++)
                {
                    var a = Vector2.Transform(joints[i], toWorld);
                    var b = Vector2.Transform(joints[i + 1], toWorld);
                    var d = b - a;
                    var len = d.Length();
                    if (len < 1e-5f)
                        continue;

                    var t = Math.Clamp((total - n + i + 0.5f) / total, 0f, 1f);
                    var thick = comp.RootThickness + (comp.FootThickness - comp.RootThickness) * t;
                    var mid = (a + b) * 0.5f;

                    // Небольшой нахлёст закрывает щели на изгибах.
                    var box = Box2.CenteredAround(mid, new Vector2(len + thick * 0.5f, thick));
                    var angle = new Angle(MathF.Atan2(d.Y, d.X));
                    handle.DrawTextureRect(segTex, new Box2Rotated(box, angle, mid));

                    if (jointTex != null && i > 0)
                    {
                        var js = thick * 1.2f;
                        handle.DrawTextureRect(jointTex, Box2.CenteredAround(a, new Vector2(js, js)));
                    }
                }

                if (footTex != null)
                {
                    var foot = Vector2.Transform(joints[n], toWorld);
                    var dir = Vector2.Transform(joints[n], toWorld) - Vector2.Transform(joints[n - 1], toWorld);
                    var angle = new Angle(MathF.Atan2(dir.Y, dir.X));
                    // Поднятая стопа "ближе к камере" — рисуем её крупнее.
                    var size = comp.FootSize * (1f + 0.35f * st.Height);
                    var box = Box2.CenteredAround(foot, new Vector2(size, size));
                    handle.DrawTextureRect(footTex, new Box2Rotated(box, angle, foot));
                }
            }
        }
    }

    private void DrawFootprints(DrawingHandleWorld handle, Robust.Shared.Map.MapId mapId)
    {
        foreach (var fp in _legs.Footprints)
        {
            if (!_ent.TryGetComponent(fp.Parent, out TransformComponent? parentXform) || parentXform.MapID != mapId)
                continue;

            var (parentPos, parentRot) = _xform.GetWorldPositionRotation(parentXform);
            var pos = parentPos + parentRot.RotateVec(fp.Position);

            // Полная яркость, затем плавное исчезновение за последние FadeTime секунд.
            var remaining = fp.Lifetime - fp.Age;
            var fade = Math.Clamp(remaining / fp.FadeTime, 0f, 1f);
            var color = fp.Color.WithAlpha(fp.Color.A * fade);

            var tex = GetTexture(fp.Sprite, fp.State);
            var box = Box2.CenteredAround(pos, new Vector2(fp.Size, fp.Size));
            handle.DrawTextureRect(tex, new Box2Rotated(box, parentRot + fp.Rotation, pos), color);
        }
    }
}
