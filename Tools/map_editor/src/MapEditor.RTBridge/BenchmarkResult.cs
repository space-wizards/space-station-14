using System.Collections.Generic;

namespace MapEditor.RTBridge;

/// <summary>
///     Result of one phase of an automated render benchmark. Aggregates
///     a window of per frame samples (FPS, draw calls, batches) into
///     averages plus min/max so a wide shot vs a close up vs a
///     mid pan can be compared at a glance.
/// </summary>
public sealed record BenchmarkPhase(
    string Name,
    int SampleCount,
    double FpsAvg,
    double FrameTimeMsAvg,
    double FrameTimeMsMax,
    double SpriteDrawCallsAvg,
    double GlDrawCallsAvg,
    double BatchCountAvg,
    double LargestBatchVerticesAvg)
{
    public override string ToString()
    {
        return
            $"{Name,-22} fps={FpsAvg,6:F1}  ft={FrameTimeMsAvg,6:F2}ms (max {FrameTimeMsMax,6:F2})  " +
            $"clyDC={SpriteDrawCallsAvg,8:F0}  glDC={GlDrawCallsAvg,6:F0}  " +
            $"batches={BatchCountAvg,6:F0}  largestBatchV={LargestBatchVerticesAvg,7:F0}  " +
            $"(n={SampleCount})";
    }
}

/// <summary>
///     Aggregated result of a full benchmark run, with one entry per
///     phase plus a summary line that can be dumped to status text or a
///     log file.
/// </summary>
public sealed record BenchmarkResult(
    string MapLabel,
    int VisibleEntityCountAtWideShot,
    IReadOnlyList<BenchmarkPhase> Phases)
{
    public string FormatReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Render benchmark: ").Append(MapLabel).Append('\n');
        sb.Append("Wide shot visible entities (approx): ").Append(VisibleEntityCountAtWideShot).Append('\n');
        sb.Append('\n');
        foreach (var phase in Phases)
        {
            sb.Append(phase).Append('\n');
        }
        return sb.ToString();
    }
}
