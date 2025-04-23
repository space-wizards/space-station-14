using System.Text;

namespace Content.Server.Starlight.TTS;

public static class NumberConverter
{
    private static readonly string[] Units =
    [
        "", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven",
        "twelve", "thirteen", "fourteen", "fifteen",
        "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "ten", "twenty", "thirty", "forty", "fifty",
        "sixty", "seventy", "eighty", "ninety"
    ];

    private static readonly string[] Scales =
    ["", "thousand", "million", "billion", "trillion"];

    public static string NumberToText(long number)
    {
        if (number == 0)
            return "zero";

        if (number < 0)
            return "minus " + NumberToText(-number);

        var words = new StringBuilder();

        var unit = 0;

        while (number > 0)
        {
            if (number % 1000 != 0)
            {
                var chunk = new StringBuilder();

                var hundreds = (int)(number % 1000 / 100);
                var tensUnits = (int)(number % 100);

                if (hundreds != 0)
                    chunk.Append(Units[hundreds] + " hundred");

                if (tensUnits > 0)
                {
                    if (hundreds != 0)
                        chunk.Append(" and ");

                    if (tensUnits < 20)
                        chunk.Append(Units[tensUnits]);
                    else
                    {
                        var tens = tensUnits / 10;
                        var units = tensUnits % 10;

                        chunk.Append(Tens[tens]);

                        if (units != 0)
                            chunk.Append("-" + Units[units]);
                    }
                }

                chunk.Append(" " + Scales[unit]);

                if (words.Length > 0)
                    chunk.Append(", ");

                words.Insert(0, chunk);
            }

            number /= 1000;
            unit++;
        }

        return words.ToString().Trim();
    }
}
