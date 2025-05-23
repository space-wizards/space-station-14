//starlight maintained file, this is needed for the DB rework of markings
using System.Text.Json;
using Content.Shared.Humanoid.Markings;

namespace Content.Server.Humanoid.Markings.Extensions;

public static class MarkingExtensions
{
        //refactored all this shit code to properly use json to pass things around instead of what it was doing before
        //should be much more stable for future wizden changes and whatever else we want to do
        public static string ToDBString(this Marking Marking)
        {
            // reserved character
            string sanitizedName = Marking.MarkingId.Replace('@', '_');
            List<string> colorStringList = new();
            foreach (Color color in Marking.MarkingColors)
                colorStringList.Add(color.ToHex());

            var json = JsonSerializer.Serialize(new MarkingDbString
            {
                MarkingId = sanitizedName,
                Colors = colorStringList,
                IsGlowing = Marking.IsGlowing
            });

            return json;

            //return $"{sanitizedName}@{String.Join(',', colorStringList)}@{IsGlowing}";
        }

        //dummy object definition to pass around
        private sealed class MarkingDbString
        {
            public string MarkingId { get; set; } = default!;
            public List<string> Colors { get; set; } = new();
            public bool IsGlowing { get; set; } = false;
        }

        public static Marking? ParseFromDbString(string input)
        {
            List<Color> colorList;

            //first we need to decide if this string is in the old format or not
            //so try to parse it as a json object first
            if (IsJsonValid(input))
            {
                var json = JsonSerializer.Deserialize<MarkingDbString>(input);

                if (json == null) return null;

                colorList = new();
                foreach (string color in json.Colors)
                    colorList.Add(Color.FromHex(color));

                return new Marking(json.MarkingId, colorList, json.IsGlowing);
            }

            if (input.Length == 0) return null;
            var split = input.Split('@');
            if (split.Length < 2) return null;
            colorList = new();
            foreach (string color in split[1].Split(','))
                colorList.Add(Color.FromHex(color));

            return new Marking(split[0], colorList, false);
        }

        public static bool IsJsonValid(string txt)
        {
            try { return JsonDocument.Parse(txt) != null; } catch { return false; }
        }
}
