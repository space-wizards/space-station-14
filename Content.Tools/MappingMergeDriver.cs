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

            if (ours.GridsNode.Children.Count != 1 || based.GridsNode.Children.Count != 1 || other.GridsNode.Children.Count != 1)
            {
                Console.WriteLine("one or more files had an amount of grids not equal to 1");
                Environment.Exit(1);
            }

            if (!(new Merger(ours, based, other).Merge()))
            {
                Console.WriteLine("unable to merge!");
                Environment.Exit(1);
            }

            ours.Save();
            Environment.Exit(0);
        }
    }
}
