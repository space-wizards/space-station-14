using System;

namespace Content.Tools
{
    internal static class MappingMergeDriver
    {
        public static void Main(string[] args)
        {
            var ourPath = args[1];
            var ours = new Map(ourPath);
            var based = args[2];
            var other = new Map(args[3]);
            var fileName = args[4];

            ours.Merge(other);
            ours.Save(ourPath);

            Environment.Exit(0);
        }
    }
}
