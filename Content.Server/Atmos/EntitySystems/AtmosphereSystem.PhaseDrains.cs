using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        private delegate void TileWorker<TContext>(AtmosphereSystem self, TContext ctx, TileAtmosphere tile);

        private delegate void QueueWorker<TContext, TItem>(AtmosphereSystem self, TContext ctx, TItem item);

        private bool BudgetExhausted => _simulationStopwatch.Elapsed.TotalMilliseconds >= AtmosMaxProcessTime;

        private bool OverBudget(ref int sinceLastCheck, int lagCheck = LagCheckIterations)
        {
            if (sinceLastCheck++ < lagCheck)
                return false;
            sinceLastCheck = 0;
            return BudgetExhausted;
        }

        private static void RefillScratch<T>(HashSet<T> source, Queue<T> scratch)
        {
            scratch.Clear();
            scratch.EnsureCapacity(source.Count);
            foreach (var item in source)
                scratch.Enqueue(item);
        }

        // The queue is the resume curssor.
        private bool DrainScratch<TContext, TItem>(
            Queue<TItem> scratch,
            QueueWorker<TContext, TItem> worker,
            TContext context,
            int lagCheck = LagCheckIterations)
        {
            var number = 0;
            while (scratch.TryDequeue(out var item))
            {
                worker(this, context, item);
                if (OverBudget(ref number, lagCheck))
                    return false;
            }

            return true;
        }

        // A pause cannot overwrite another phase's snapshot.
        private bool DrainTilesBatched<TContext>(
            GridAtmosphereComponent atmosphere,
            TileRunState run,
            HashSet<TileAtmosphere> source,
            TileWorker<TContext> worker,
            TContext context,
            int lagCheck = LagCheckIterations)
        {
            if (!atmosphere.Processing.ProcessingPaused)
            {
                // Resumes re-enter with ProcessingPaused set.
                if (source.Count == 0)
                    return true;

                run.Tiles.Clear();
                run.Tiles.AddRange(source);
                run.Cursor = 0;
            }

            var tiles = run.Tiles;
            var number = 0;
            while (run.Cursor < tiles.Count)
            {
                var tile = tiles[run.Cursor++];
                worker(this, context, tile);

                if (OverBudget(ref number, lagCheck))
                    return false;
            }

            return true;
        }

        // Queue phases also use scratch.
        private bool DrainQueueBatched<TContext, TItem>(
            GridAtmosphereComponent atmosphere,
            HashSet<TItem> source,
            Queue<TItem> scratch,
            QueueWorker<TContext, TItem> worker,
            TContext context,
            int lagCheck = LagCheckIterations)
        {
            if (!atmosphere.Processing.ProcessingPaused)
            {
                if (source.Count == 0)
                    return true;

                RefillScratch(source, scratch);
            }

            return DrainScratch(scratch, worker, context, lagCheck);
        }

    }
}
