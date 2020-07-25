using System;
using System.IO;

namespace Content.Tools
{
    internal static class MappingMergeDriver
    {
        public static void Main(string[] args)
        {
            var ours = new Map(args[1]);
            var @based = new Map(args[2]);
            var other = new Map(args[3]);
            var fileName = args[4];

            ours.Merge(other);
            ours.Save(fileName);

            Environment.Exit(0);
        }
    }
}
