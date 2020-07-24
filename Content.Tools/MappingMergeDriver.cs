namespace Content.Tools
{
    internal static class MappingMergeDriver
    {
        public static void Main(string[] args)
        {
            var ours = new Map(args[1]);
            var @based = new Map(args[2]);
            var other = new Map(args[3]);

            ours.Merge(other);
        }
    }
}
