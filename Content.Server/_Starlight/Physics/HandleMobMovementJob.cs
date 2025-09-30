using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared._Starlight.Sound;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Physics;

public struct HandleMobMovementJob(SLMoverController moverController) : IParallelRobustJob
{
    public readonly int BatchSize => 32;

    public float FrameTime;

    private List<Entity<InputMoverComponent>> _input = [];
    private Vector2?[] _velocities = [];
    private Angle?[] _rotations = [];
    private SoundEvent?[] _sounds = [];

    public void Prepare(List<Entity<InputMoverComponent>> entities)
    {
        _input = entities;
        EnsureCapacity(ref _velocities, entities.Count);
        EnsureCapacity(ref _rotations, entities.Count);
        EnsureCapacity(ref _sounds, entities.Count);
    }

    public readonly Span<Vector2?> Velocities => _velocities.AsSpan(.._input.Count);
    public readonly Span<Angle?> Rotations => _rotations.AsSpan(.._input.Count);
    public readonly Span<SoundEvent?> Sounds => _sounds.AsSpan(.._input.Count);

    public void Execute(int index)
    {
        try
        {
            var velocity = moverController.HandleAIMobMovement(_input[index], FrameTime, out var playSound, out var rotation);
            _velocities[index] = velocity;
            _sounds[index] = playSound;
            _rotations[index] = rotation;
        }
        catch (Exception ex)
        {

        }
    }

    static void EnsureCapacity<T>(ref T[] arr, int needed)
    {
        if (arr is null) { arr = new T[BitOperations.RoundUpToPowerOf2((uint)needed)]; return; }
        if (arr.Length >= needed) return;
        Array.Resize(ref arr, (int)BitOperations.RoundUpToPowerOf2((uint)needed));
    }
}
