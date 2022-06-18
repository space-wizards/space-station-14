using Content.Server.Shuttles.Components;
using Content.Shared.GameTicking;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    /*
     * This is a way to move a shuttle from one location to another, via an intermediate map for fanciness.
     */

    private MapId? _hyperSpaceMap;

    private const float HyperspaceStartupTime = 4.5f;
    private const float DefaultHyperspaceTime = 30f;

    private SoundSpecifier _startupSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_begin.ogg");
    // private SoundSpecifier _travelSound = new SoundPathSpecifier();
    private SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_end.ogg");

    /// <summary>
    /// Moves a shuttle from its current position to the target one. Goes through the hyperspace map while the timer is running.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="coordinates"></param>
    /// <param name="timer"></param>
    public void Hyperspace(ShuttleComponent component, EntityCoordinates coordinates, float timer = HyperspaceStartupTime)
    {
        if (HasComp<HyperspaceComponent>(component.Owner))
        {
            _sawmill.Warning($"Tried queuing {ToPrettyString(component.Owner)} which already has HyperspaceComponent?");
            return;
        }

        var hyperspace = AddComp<HyperspaceComponent>(component.Owner);
        hyperspace.Accumulator = timer;
        // TODO: Need BroadcastGrid to not be bad.
        SoundSystem.Play(_startupSound.GetSound(), Filter.Pvs(component.Owner, GetSoundRange(component.Owner), entityManager: EntityManager), _startupSound.Params);
    }

    private void UpdateHyperspace(float frameTime)
    {
        foreach (var comp in EntityQuery<HyperspaceComponent>())
        {
            comp.Accumulator -= frameTime;

            if (comp.Accumulator > 0f) continue;

            var xform = Transform(comp.Owner);

            switch (comp.State)
            {
                case HyperspaceState.Starting:
                    comp.State = HyperspaceState.Travelling;
                    SetupHyperspace();
                    xform.Coordinates = new EntityCoordinates(_mapManager.GetMapEntityId(_hyperSpaceMap!.Value), Vector2.Zero);
                    comp.Accumulator += DefaultHyperspaceTime;
                    break;
                case HyperspaceState.Travelling:
                    xform.Coordinates = comp.TargetCoordinates;
                    SoundSystem.Play(_arrivalSound.GetSound(),
                        Filter.Pvs(comp.Owner, GetSoundRange(comp.Owner), entityManager: EntityManager));
                    RemComp<HyperspaceComponent>(comp.Owner);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private float GetSoundRange(EntityUid uid)
    {
        if (!_mapManager.TryGetGrid(uid, out var grid)) return 4f;

        return MathF.Max(grid.LocalAABB.Width, grid.LocalAABB.Height) + 12.5f;
    }

    private void SetupHyperspace()
    {
        if (_hyperSpaceMap != null) return;

        _hyperSpaceMap = _mapManager.CreateMap();
        _sawmill.Info($"Setup hyperspace map at {_hyperSpaceMap.Value}");
        DebugTools.Assert(!_mapManager.IsMapPaused(_hyperSpaceMap.Value));
    }

    private void CleanupHyperspace()
    {
        if (_hyperSpaceMap == null) return;
        _mapManager.DeleteMap(_hyperSpaceMap.Value);
        _hyperSpaceMap = null;
    }
}
