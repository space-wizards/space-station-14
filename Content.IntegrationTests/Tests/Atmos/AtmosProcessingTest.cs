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
                counterBefore = doomedEnt.Comp1.CycleCounter;

                // Hold the component instance: after deletion we inspect the same object the
                // bail-out path mutated, not a re-resolution of the dead entity.
                var doomedAtmos = doomedEnt.Comp1;
                SEntMan.DeleteEntity(doomedEnt.Owner);
                state = SAtmos.ProcessAtmosphereOnce(doomedEnt, mapUid, SAtmos.AtmosTime);

                counterAfter = doomedAtmos.CycleCounter;
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
                "Deleted grid advanced the freshness marker on abandonment.");
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
                BeginPausedCycle();

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
    public async Task SkippedGridResetsInFlightCycle()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorSetBeforeSkip = false;
        var pausedBeforeSkip = false;
        var counterBefore = 0;
        var cursorCleared = false;
        var pausedAfterSkip = true;
        var queueCountAfterSkip = -1;
        var invalidatedCountAfterSkip = 0;
        var counterAfter = 0;

        try
        {
            await Server.WaitPost(() =>
            {
                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                BeginPausedCycle();

                cursorSetBeforeSkip = atmos.Processing.CycleCursor is not null;
                pausedBeforeSkip = atmos.Processing.ProcessingPaused;
                counterBefore = atmos.CycleCounter;

                // Generous budget so other grids cannot starve the scheduler before it skips ours.
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                SAtmos.SetAtmosphereSimulation((ProcessEnt.Owner, atmos), false);
            });

            await Server.WaitRunTicks(1);

            await Server.WaitPost(() =>
            {
                cursorCleared = atmos.Processing.CycleCursor is null;
                pausedAfterSkip = atmos.Processing.ProcessingPaused;
                queueCountAfterSkip = atmos.Processing.CurrentRunInvalidatedTiles.Count;
                invalidatedCountAfterSkip = atmos.InvalidatedCoords.Count;
                counterAfter = atmos.CycleCounter;
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                SAtmos.SetAtmosphereSimulation((ProcessEnt.Owner, atmos), true);
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            });
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorSetBeforeSkip, Is.True,
                "Expected an in-flight cycle before the skip.");
            Assert.That(pausedBeforeSkip, Is.True,
                "Expected a paused phase before the skip.");
            Assert.That(cursorCleared, Is.True,
                "Skipped grid kept its cycle cursor.");
            Assert.That(pausedAfterSkip, Is.False,
                "Skipped grid kept ProcessingPaused set.");
            Assert.That(queueCountAfterSkip, Is.EqualTo(0),
                "Skipped grid kept stale resume work.");
            Assert.That(invalidatedCountAfterSkip, Is.GreaterThan(0),
                "Skipped grid lost pending revalidation work.");
            Assert.That(counterAfter, Is.EqualTo(counterBefore),
                "Skipped grid advanced the freshness marker on abandonment.");
        }
    }

    [Test]
    public async Task AbandonedCycleDoesNotReuseFreshnessMarker()
    {
        var atmos = ProcessEnt.Comp1;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorSetBeforeSkip = false;
        var markerWhileAbandoned = 0;
        var cursorClearedAfterSkip = false;
        var markerOfNextRun = 0;

        try
        {
            await Server.WaitPost(() =>
            {
                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                // Start a cycle and pause it mid-flight; this run owns the current marker.
                BeginPausedCycle();

                cursorSetBeforeSkip = atmos.Processing.CycleCursor is not null;
                markerWhileAbandoned = atmos.CycleCounter;

                // Desimulate so the in-flight cycle is abandoned, not resumed in place.
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                SAtmos.SetAtmosphereSimulation((ProcessEnt.Owner, atmos), false);
            });

            await Server.WaitRunTicks(1);

            await Server.WaitPost(() =>
            {
                cursorClearedAfterSkip = atmos.Processing.CycleCursor is null;

                // Re-simulate and start the next cycle; it must not reuse the abandoned run's marker.
                SAtmos.SetAtmosphereSimulation((ProcessEnt.Owner, atmos), true);
                BeginPausedCycle();

                markerOfNextRun = atmos.CycleCounter;
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                SAtmos.SetAtmosphereSimulation((ProcessEnt.Owner, atmos), true);
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            });
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cursorSetBeforeSkip, Is.True,
                "Expected an in-flight cycle before the skip.");
            Assert.That(cursorClearedAfterSkip, Is.True,
                "Skipped grid kept its cycle cursor.");
            Assert.That(markerOfNextRun, Is.GreaterThan(markerWhileAbandoned),
                "Cycle after an abandoned run reused its freshness marker; stale tile markers will collide.");
        }
    }

    [Test]
    public async Task MidCycleCVarFlipDoesNotMutateInFlightCycle()
    {
        var atmos = ProcessEnt.Comp1;
        var savedExcited = Server.CfgMan.GetCVar(CCVars.ExcitedGroups);
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);
        var cursorSet = false;
        var flagsBeforeFlip = AtmosPhases.ExcitedGroups;
        var flagsAfterFlip = AtmosPhases.ExcitedGroups;

        await Server.WaitPost(() =>
        {
            try
            {
                Server.CfgMan.SetCVar(CCVars.ExcitedGroups, false);
                SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

                BeginPausedCycle();

                cursorSet = atmos.Processing.CycleCursor is not null;
                flagsBeforeFlip = (atmos.Processing.CycleCursor?.Flags ?? AtmosPhases.None) & AtmosPhases.ExcitedGroups;

                Server.CfgMan.SetCVar(CCVars.ExcitedGroups, true);

                flagsAfterFlip = (atmos.Processing.CycleCursor?.Flags ?? AtmosPhases.None) & AtmosPhases.ExcitedGroups;
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
            Assert.That(flagsBeforeFlip, Is.EqualTo(AtmosPhases.None),
                "Cycle snapshot did not match the initial CVar value.");
            Assert.That(flagsAfterFlip, Is.EqualTo(AtmosPhases.None),
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

    // Queues revalidation work, zeroes the budget, and runs one ProcessAtmosphere call so the grid is
    // left holding an in-flight cycle paused on the budget. Leaves the budget at zero for the caller.
    private void BeginPausedCycle()
    {
        QueueRevalidateWork(MapData.Grid.Owner);
        Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
        SAtmos.ProcessAtmosphereOnce(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);
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

    [Test]
    public async Task TileDrainResumeVisitsEachTileExactlyOnce()
    {
        var atmos = ProcessEnt.Comp1;
        var run = atmos.Processing.ActiveTilesRun;
        var savedBudget = Server.CfgMan.GetCVar(CCVars.AtmosMaxProcessTime);

        var snapshotCount = 0;
        var distinctCount = -1;
        var cursorAtPause = -1;
        var snapshotStableAcrossResume = false;
        var finalCursor = -1;

        await Server.WaitPost(() =>
        {
            SAtmos.RunProcessingFull(ProcessEnt, MapData.MapUid, SAtmos.AtmosTime);

            try
            {
                SAtmos.GetAllMixtures(ProcessEnt.Owner, excite: true).Count();
                SAtmos.SetProcessingState((ProcessEnt.Owner, atmos), AtmosphereProcessingState.ActiveTiles);

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 0f);
                SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.ActiveTiles);

                // Snapshot captured when the phase first ran; resume must continue it, not rebuild it.
                var pausedSnapshot = run.Tiles.ToList();
                snapshotCount = pausedSnapshot.Count;
                distinctCount = pausedSnapshot.Distinct().Count();
                cursorAtPause = run.Cursor;

                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, 100f);
                SAtmos.RunProcessingStage(ProcessEnt, AtmosphereProcessingState.ActiveTiles);

                snapshotStableAcrossResume = run.Tiles.SequenceEqual(pausedSnapshot);
                finalCursor = run.Cursor;
            }
            finally
            {
                Server.CfgMan.SetCVar(CCVars.AtmosMaxProcessTime, savedBudget);
            }
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(snapshotCount, Is.GreaterThan(31),
                "Expected a snapshot larger than the lag-check granularity.");
            Assert.That(distinctCount, Is.EqualTo(snapshotCount),
                "Tile snapshot contained duplicate tiles.");
            Assert.That(cursorAtPause, Is.InRange(1, snapshotCount - 1),
                "Phase did not pause partway through the snapshot.");
            Assert.That(snapshotStableAcrossResume, Is.True,
                "Resume rebuilt the tile snapshot instead of continuing it.");
            Assert.That(finalCursor, Is.EqualTo(snapshotCount),
                "Resume did not drain every tile exactly once.");
        }
    }
}
