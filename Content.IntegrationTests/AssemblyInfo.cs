[assembly: Parallelizable(ParallelScope.Children)]

// Parallelism was previously capped at 2 due to JIT lock contention with expression trees:
// https://github.com/dotnet/runtime/issues/107197
[assembly: LevelOfParallelism(4)]
