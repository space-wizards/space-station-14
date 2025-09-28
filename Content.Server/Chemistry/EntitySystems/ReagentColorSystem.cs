using Content.Server.Light.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Sprite;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Chemistry.EntitySystems;

/// <summary>
/// This system handles updating the sprite and light color of entities
/// with a <see cref="ReagentColorComponent"/> to match their solution's color.
/// </summary>
public sealed class ReagentColorSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReagentColorComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ReagentColorComponent> entity, ref ComponentStartup args)
    {
        UpdateColor(entity);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ReagentColorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_solutionContainer.ResolveSolution(uid, comp.SolutionName, ref comp.Solution))
            {
                UpdateColor((uid, comp));
            }
        }
    }

    private void UpdateColor(Entity<ReagentColorComponent> entity)
    {
        var (uid, comp) = entity;
        if (!_solutionContainer.TryGetSolution(uid, comp.SolutionName, out _, out var solution))
            return;

        var color = solution.GetColor(_prototypeManager);
        if (color.A <= 0f)
            return;

        // Update point light color
        if (TryComp<PointLightComponent>(uid, out var light) && light.Color != color)
        {
            _light.SetColor(uid, color, light);
        }

        // Update random sprite colors (for anomalies)
        if (TryComp<RandomSpriteComponent>(uid, out var randomSprite))
        {
            var changed = false;
            // Create a copy of the keys to iterate over, as the dictionary might be modified.
            var keys = randomSprite.Selected.Keys.ToList();
            foreach (var key in keys)
            {
                var state = randomSprite.Selected[key];
                if (state.Color != color)
                {
                    state.Color = color;
                    randomSprite.Selected[key] = state;
                    changed = true;
                }
            }
            if (changed)
                Dirty(uid, randomSprite);
        }

        // Update appearance for sprite coloring (for slimes)
        if (HasComp<AppearanceComponent>(uid))
        {
            _appearance.SetData(uid, ReagentColorVisuals.Color, color);
        }
    }
}
