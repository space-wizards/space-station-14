using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Content.Shared.Text
{
    public static class Names
    {
        public static readonly IReadOnlyList<string> MaleFirstNames;
        public static readonly IReadOnlyList<string> FemaleFirstNames;
        public static readonly IReadOnlyList<string> LastNames;

        static Names()
        {
            MaleFirstNames = ResourceToLines("Content.Shared.Text.Names.first_male.txt");
            FemaleFirstNames = ResourceToLines("Content.Shared.Text.Names.first_female.txt");
            LastNames = ResourceToLines("Content.Shared.Text.Names.last.txt");
        }

        private static string[] ResourceToLines(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            return reader
                .ReadToEnd()
                .Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
