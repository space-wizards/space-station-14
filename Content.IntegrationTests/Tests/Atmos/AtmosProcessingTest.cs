using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Tests.Helpers;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

public sealed class AtmosDeviceUpdateListenerSystem : TestListenerSystem<AtmosDeviceUpdateEvent>;

public sealed class AtmosProcessingTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    public async Task DeviceUpdatesOncePerCycleWithCorrectDt()
    {
        const int cycles = 8;

        var probe = EntityUid.Invalid;
        try
        {
            await Server.WaitPost(() =>
            {
                probe = SpawnProbe();

                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
                ClearEvents<AtmosDeviceUpdateEvent>(probe);

                for (var i = 0; i < cycles; i++)
                    SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
            });

            await Server.WaitAssertion(() =>
            {
                var events = GetListenerSystem<AtmosDeviceUpdateEvent>().GetEvents(probe).ToList();

                Assert.That(events, Has.Count.EqualTo(cycles),
                    "Device received the wrong number of update events.");

                foreach (var ev in events)
                {
                    Assert.That(ev.dt, Is.EqualTo(SAtmos.AtmosTime).Within(1e-6f),
                        "Device update dt did not match AtmosTime.");
                    Assert.That(ev.Grid, Is.Not.Null,
                        "Device update did not carry a grid.");
                    Assert.That(ev.Map, Is.Not.Null,
                        "Device update did not carry a map atmosphere.");
                }

                Assert.That(events.Sum(e => e.dt), Is.EqualTo(cycles * SAtmos.AtmosTime).Within(1e-4f),
                    "Device update dt was inflated across processing phases.");
            });
        }
        finally
        {
            await DeleteProbe(probe);
        }
    }

    [Test]
    public async Task RevalidatePausesAndResumesOnBudgetExhaustion()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);

        var invalidatedCount = 0;
        var initiallyPaused = true;
        var firstFinished = true;
        var firstPaused = false;
        var firstQueueCount = 0;
        var secondFinished = false;
        var secondPaused = true;
        var secondQueueCount = -1;

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

            try
            {
                QueueRevalidateWork(MapData.Grid.Owner);

                invalidatedCount = atmos.InvalidatedCoords.Count;
                initiallyPaused = atmos.Processing.ProcessingPaused;

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                firstFinished = SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.Revalidate);
                firstPaused = atmos.Processing.ProcessingPaused;
                firstQueueCount = atmos.Processing.CurrentRunInvalidatedTiles.Count;

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                secondFinished = SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.Revalidate);
                secondPaused = atmos.Processing.ProcessingPaused;
                secondQueueCount = atmos.Processing.CurrentRunInvalidatedTiles.Count;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(invalidatedCount, Is.GreaterThan(0),
                "InvalidatedCoords was empty.");
            Assert.That(initiallyPaused, Is.False);
            Assert.That(firstFinished, Is.False,
                "Revalidate did not yield under budget pressure.");
            Assert.That(firstPaused, Is.True,
                "Revalidate did not leave ProcessingPaused set.");
            Assert.That(firstQueueCount, Is.GreaterThan(0),
                "Revalidate drained the resume queue after yielding.");
            Assert.That(secondFinished, Is.True,
                "Revalidate did not finish after budget was restored.");
            Assert.That(secondPaused, Is.False,
                "Revalidate left ProcessingPaused set after finishing.");
            Assert.That(secondQueueCount, Is.EqualTo(0),
                "Revalidate left pending resume work.");
        }
    }

    [Test]
    public async Task DeviceDtScalesAcrossBudgetPause()
    {
        var probe = EntityUid.Invalid;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var iterations = 0;
        var state = AtmosphereProcessingCompletionState.Continue;
        var residualDtAfterFinish = 0f;
        const int maxIterations = 50;

        try
        {
            await Server.WaitPost(() =>
            {
                probe = SpawnProbe();

                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
                ClearEvents<AtmosDeviceUpdateEvent>(probe);

                try
                {
                    QueueRevalidateWork(MapData.Grid.Owner);
                    Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);

                    do
                    {
                        state = SAtmos.ProcessAtmosphereOnce(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
                        iterations++;
                    } while (state != AtmosphereProcessingCompletionState.Finished && iterations < maxIterations);

                    // Capture the post-snapshot accumulation while we still own the simulation.
                    residualDtAfterFinish = ProcessEnt.Comp1.Processing.TimeSinceLastDeviceUpdate;
                }
                finally
                {
                    Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
                }
            });

            Assert.That(state, Is.EqualTo(AtmosphereProcessingCompletionState.Finished));
            Assert.That(iterations, Is.GreaterThan(1),
                "Expected the cycle to span multiple calls.");

            await Server.WaitAssertion(() =>
            {
                var events = GetListenerSystem<AtmosDeviceUpdateEvent>().GetEvents(probe).ToList();
                Assert.That(events, Has.Count.EqualTo(1),
                    "Paused cycle emitted the wrong number of device events.");
                // dt snaps when AtmosDevices first runs.
                Assert.That(events[0].dt + residualDtAfterFinish,
                    Is.EqualTo(iterations * SAtmos.AtmosTime).Within(1e-4f),
                    "Paused cycle dt accounting was wrong.");
                Assert.That(events[0].dt, Is.GreaterThan(SAtmos.AtmosTime),
                    "Paused cycle did not accumulate more than one frame of dt.");
            });
        }
        finally
        {
            await DeleteProbe(probe);
        }
    }

    [Test]
    public async Task RunProcessingFullDoesNotInflateDeviceDtAcrossInternalResume()
    {
        var probe = EntityUid.Invalid;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);

        try
        {
            await Server.WaitPost(() =>
            {
                probe = SpawnProbe();

                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
                ClearEvents<AtmosDeviceUpdateEvent>(probe);

                try
                {
                    QueueRevalidateWork(MapData.Grid.Owner);
                    Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);

                    SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
                }
                finally
                {
                    Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
                }
            });

            await Server.WaitAssertion(() =>
            {
                var events = GetListenerSystem<AtmosDeviceUpdateEvent>().GetEvents(probe).ToList();
                Assert.That(events, Has.Count.EqualTo(1),
                    "RunProcessingFull emitted the wrong number of device events.");
                Assert.That(events[0].dt, Is.EqualTo(SAtmos.AtmosTime).Within(1e-6f),
                    "RunProcessingFull inflated device dt while resuming.");
            });
        }
        finally
        {
            await DeleteProbe(probe);
        }
    }

    [Test]
    public async Task EntityDeletedMidCycleYieldsCleanly()
    {
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorWasSet = false;
        var cursorCleared = false;
        var counterBefore = 0;
        var counterAfter = 0;
        var state = AtmosphereProcessingCompletionState.Finished;

        await Server.WaitPost(() =>
        {
            Entity<GridAtmosphereComponent, GasTileOverlayComponent, MapGridComponent, TransformComponent> doomedEnt = default;
            var mapUid = EntityUid.Invalid;

            try
            {
                var mapId = SEntMan.GetComponent<TransformComponent>(MapData.Grid).MapID;
                var grid = MapMan.CreateGridEntity(mapId);
                var atmos = SEntMan.EnsureComponent<GridAtmosphereComponent>(grid.Owner);
                var overlay = SEntMan.EnsureComponent<GasTileOverlayComponent>(grid.Owner);
                var xform = SEntMan.GetComponent<TransformComponent>(grid.Owner);
                doomedEnt = (grid.Owner, atmos, overlay, grid.Comp, xform);
                mapUid = xform.MapUid!.Value;

                QueueRevalidateWork(grid.Owner);

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                SAtmos.ProcessAtmosphereOnce(doomedEnt, mapUid, SAtmos.AtmosTime);

                cursorWasSet = doomedEnt.Comp1.Processing.CycleCursor is not null;
                counterBefore = doomedEnt.Comp1.UpdateCounter;

                // Hold the component instance: after deletion we inspect the same object the
                // bail-out path mutated, not a re-resolution of the dead entity.
                var doomedAtmos = doomedEnt.Comp1;
                SEntMan.DeleteEntity(doomedEnt.Owner);
                state = SAtmos.ProcessAtmosphereOnce(doomedEnt, mapUid, SAtmos.AtmosTime);

                counterAfter = doomedAtmos.UpdateCounter;
                cursorCleared = doomedAtmos.Processing.CycleCursor is null;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorWasSet, Is.True,
                "Expected an in-flight cycle before deletion.");
            Assert.That(state, Is.EqualTo(AtmosphereProcessingCompletionState.Continue),
                "Deleted grid returned the wrong completion state.");
            Assert.That(counterAfter, Is.EqualTo(counterBefore),
                "Deleted grid advanced UpdateCounter.");
            Assert.That(cursorCleared, Is.True,
                "Deleted grid left the cycle cursor set.");
        }
    }

    [Test]
    public async Task TimerDoesNotDriftAcrossPause()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var timerAfterPausedIterations = 0f;
        var timerAfterFinish = 0f;

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

            try
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);

                // 50 = enough paused iterations to expose any per-iteration timer drift.
                for (var i = 0; i < 50; i++)
                    SAtmos.ProcessAtmosphereOnce(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                timerAfterPausedIterations = atmos.Processing.Timer;

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                timerAfterFinish = atmos.Processing.Timer;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        Assert.That(timerAfterPausedIterations, Is.LessThan(SAtmos.AtmosTime),
            $"Timer drifted across paused iterations: {timerAfterPausedIterations}.");
        Assert.That(timerAfterFinish, Is.InRange(0f, SAtmos.AtmosTime),
            $"Timer was out of range after the paused cycle: {timerAfterFinish}.");
    }

    [Test]
    public async Task RebuildGridAtmosphereResetsCycleState()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorInFlightBeforeRebuild = false;
        var pausedBeforeRebuild = false;
        var accumulatedTimeBeforeRebuild = 0f;
        var timerAfterRebuild = -1f;
        var cursorCleared = false;
        var pausedAfterRebuild = true;
        var stateAfterRebuild = AtmosphereProcessingState.NumStates;
        var timeSinceLastDeviceUpdate = -1f;
        var currentRunDeviceDt = -1f;

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

            try
            {
                QueueRevalidateWork(MapData.Grid.Owner);

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                SAtmos.ProcessAtmosphereOnce(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                cursorInFlightBeforeRebuild = atmos.Processing.CycleCursor is not null;
                pausedBeforeRebuild = atmos.Processing.ProcessingPaused;
                accumulatedTimeBeforeRebuild = atmos.Processing.TimeSinceLastDeviceUpdate;

                SAtmos.RebuildGridAtmosphere((ProcessEnt.Owner, atmos, ProcessEnt.Comp3));

                timerAfterRebuild = atmos.Processing.Timer;
                cursorCleared = atmos.Processing.CycleCursor is null;
                pausedAfterRebuild = atmos.Processing.ProcessingPaused;
                stateAfterRebuild = atmos.State;
                timeSinceLastDeviceUpdate = atmos.Processing.TimeSinceLastDeviceUpdate;
                currentRunDeviceDt = atmos.Processing.CurrentRunDeviceDt;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorInFlightBeforeRebuild, Is.True,
                "Expected an in-flight cycle before rebuild.");
            Assert.That(pausedBeforeRebuild, Is.True,
                "Expected a paused phase before rebuild.");
            Assert.That(accumulatedTimeBeforeRebuild, Is.GreaterThan(0f));
            Assert.That(timerAfterRebuild, Is.EqualTo(0f));
            Assert.That(cursorCleared, Is.True);
            Assert.That(pausedAfterRebuild, Is.False);
            Assert.That(stateAfterRebuild, Is.EqualTo(AtmosphereProcessingState.Revalidate));
            Assert.That(timeSinceLastDeviceUpdate, Is.EqualTo(0f));
            Assert.That(currentRunDeviceDt, Is.EqualTo(0f));
        }
    }

    [Test]
    public async Task MidCycleCVarFlipDoesNotMutateInFlightCycle()
    {
        var atmos = ProcessEnt.Comp1;
        var savedExcited = Server.CfgMan.GetCVar(CCVars.ExcitedGroups);
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorSet = false;
        var flagsBeforeFlip = AtmosPhaseFlags.ExcitedGroups;
        var flagsAfterFlip = AtmosPhaseFlags.ExcitedGroups;

        await Server.WaitPost(() =>
        {
            try
            {
                Server.CfgMan.SetCVar(CCVars.ExcitedGroups, false);
                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                QueueRevalidateWork(MapData.Grid.Owner);

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                SAtmos.ProcessAtmosphereOnce(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                cursorSet = atmos.Processing.CycleCursor is not null;
                flagsBeforeFlip = (atmos.Processing.CycleCursor?.Flags ?? AtmosPhaseFlags.None) & AtmosPhaseFlags.ExcitedGroups;

                Server.CfgMan.SetCVar(CCVars.ExcitedGroups, true);

                flagsAfterFlip = (atmos.Processing.CycleCursor?.Flags ?? AtmosPhaseFlags.None) & AtmosPhaseFlags.ExcitedGroups;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.ExcitedGroups, savedExcited);
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorSet, Is.True,
                "Expected cursor to be set mid-cycle.");
            Assert.That(flagsBeforeFlip, Is.EqualTo(AtmosPhaseFlags.None),
                "Cycle snapshot did not match the initial CVar value.");
            Assert.That(flagsAfterFlip, Is.EqualTo(AtmosPhaseFlags.None),
                "CVar flip mutated the in-flight cycle snapshot.");
        }
    }

    private EntityUid SpawnProbe()
    {
        Assert.That(ProcessEnt.Comp1.AtmosDevices, Is.Empty,
            "Test grid unexpectedly had atmos devices.");

        var coords = new EntityCoordinates(MapData.Grid.Owner, Vector2.Zero);
        var probe = SEntMan.SpawnAtPosition("IntegrationTestMarker", coords);
        var device = SEntMan.EnsureComponent<AtmosDeviceComponent>(probe);
        SEntMan.EnsureComponent<TestListenerComponent>(probe);

        Assert.That(device.JoinedGrid, Is.Not.Null,
            "AtmosDeviceComponent did not join a grid atmosphere.");
        Assert.That(ProcessEnt.Comp1.AtmosDevices, Has.Count.EqualTo(1),
            "Probe did not register as the only atmos device.");

        return probe;
    }

    // Invalidates an 8x8 block (64 tiles): enough work that Revalidate pauses under budget = 0
    // and enough materialized tiles to exceed the budget lag-check granularity.
    private void QueueRevalidateWork(EntityUid gridUid)
    {
        for (var x = 0; x < 8; x++)
        for (var y = 0; y < 8; y++)
            SAtmos.InvalidateTile(gridUid, new Vector2i(x, y));
    }

    private async Task DeleteProbe(EntityUid probe)
    {
        if (probe == EntityUid.Invalid)
            return;

        await Server.WaitPost(() =>
        {
            if (!SEntMan.Deleted(probe))
                SEntMan.DeleteEntity(probe);
        });
    }
}

public sealed class AtmosProcessingTilePhaseTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/tile_atmosphere_test_room.yml");

    [Test]
    public async Task TilePhasePausesAndResumesOnBudgetExhaustion()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);

        var activeCount = 0;
        var firstCompleted = true;
        var pausedFlag = false;
        var secondCompleted = false;
        var resumedPausedFlag = true;

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

            try
            {
                activeCount = SAtmos.GetAllMixtures(ProcessEnt.Owner, excite: true).Count();
                SAtmos.SetProcessingState((ProcessEnt.Owner, atmos), AtmosphereProcessingState.ActiveTiles);

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                firstCompleted = SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.ActiveTiles);
                pausedFlag = atmos.Processing.ProcessingPaused;

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                secondCompleted = SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.ActiveTiles);
                resumedPausedFlag = atmos.Processing.ProcessingPaused;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(activeCount, Is.GreaterThan(31),
                "Expected active tile count over the lag-check granularity.");
            Assert.That(firstCompleted, Is.False,
                "ActiveTiles did not yield under budget pressure.");
            Assert.That(pausedFlag, Is.True,
                "ActiveTiles did not leave ProcessingPaused set.");
            Assert.That(secondCompleted, Is.True,
                "ActiveTiles did not finish after budget was restored.");
            Assert.That(resumedPausedFlag, Is.False,
                "ActiveTiles left ProcessingPaused set after finishing.");
        }
    }
}
