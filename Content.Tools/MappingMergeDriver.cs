using System;

namespace Content.Tools
{
    internal static class MappingMergeDriver
    {
        /// %A: Our file
        /// %O: Origin (common, base) file
        /// %B: Other file
        /// %P: Actual filename of the resulting file
        public static void Main(string[] args)
        {
            var ours = new Map(args[0]);
            var based = new Map(args[1]); // On what?
            var other = new Map(args[2]);

            Merge(ours, based, other);

            Environment.Exit(0);
        }

        public static void Merge(Map ours, Map based, Map other)
        {
            var result = ours.Merge(other);

            if (result == MergeResult.Conflict)
            {
                Environment.Exit(1);
            }

            ours.Save();
        }
    }
}
