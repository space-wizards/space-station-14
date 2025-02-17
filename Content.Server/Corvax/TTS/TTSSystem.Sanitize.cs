using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled) return;
        args.Message = args.Message.Replace("+", "");
    }

    private string Sanitize(string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @"[^a-zA-Zа-яА-ЯёЁ0-9,\-+?!. ]", "");
        text = Regex.Replace(text, @"[a-zA-Z]", ReplaceLat2Cyr, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<![a-zA-Zа-яёА-ЯЁ])[a-zA-Zа-яёА-ЯЁ]+?(?![a-zA-Zа-яёА-ЯЁ])", ReplaceMatchedWord, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<=[1-90])(\.|,)(?=[1-90])", " целых ");
        text = Regex.Replace(text, @"\d+", ReplaceWord2Num);
        text = text.Trim();
        return text;
    }

    private string ReplaceLat2Cyr(Match oneChar)
    {
        if (ReverseTranslit.TryGetValue(oneChar.Value.ToLower(), out var replace))
            return replace;
        return oneChar.Value;
    }

    private string ReplaceMatchedWord(Match word)
    {
        if (WordReplacement.TryGetValue(word.Value.ToLower(), out var replace))
            return replace;
        return word.Value;
    }

    private string ReplaceWord2Num(Match word)
    {
        if (!long.TryParse(word.Value, out var number))
            return word.Value;
        return NumberConverter.NumberToText(number);
    }

    private static readonly IReadOnlyDictionary<string, string> WordReplacement =
        new Dictionary<string, string>()
        {
            {"нт", "Эн Тэ"},
            {"смо", "Эс Мэ О"},
            {"гп", "Гэ Пэ"},
            {"рд", "Эр Дэ"},
            {"гсб", "Гэ Эс Бэ"},
            {"нм", "Эн Эм"},
            {"гв", "Гэ Вэ"},
            {"нр", "Эн Эр"},
            {"нра", "Эн Эра"},
            {"нру", "Эн Эру"},
            {"гнс", "Гэ Эн Эс"},
            {"снс", "Эс Эн Эс"},
            {"км", "Кэ Эм"},
            {"кма", "Кэ Эма"},
            {"кму", "Кэ Эму"},
            {"си", "Эс И"},
            {"срп", "Эс Эр Пэ"},
            {"цк", "Цэ Ка"},
            {"гш", "Гэ Ша"},
            {"сцк", "Эс Цэ Ка"},
            {"сгш", "Эс Гэ Ша"},
            {"пцк", "Пэ Цэ Ка"},
            {"пгш", "Пэ Гэ Ша"},
            {"оцк", "О Цэ Ка"},
            {"осщ", "О Сэ Ще"},
            {"сщ", "Сэ Ще"},
            {"шцк", "Эш Цэ Ка"},
            {"ншцк", "Эн Эш Цэ Ка"},
            {"ацк", "А Цэ Ка"},
            {"асцк", "А Эс Цэ Ка"},
            {"ксо", "Кэ Эс О"},
            {"дсо", "Дэ Эс О"},
            {"рнд", "Эр Эн Дэ"},
            {"сб", "Эс Бэ"},
            {"сбтех", "Эс Бэ Тех"},
            {"рцд", "Эр Цэ Дэ"},
            {"брпд", "Бэ Эр Пэ Дэ"},
            {"рпд", "Эр Пэ Дэ"},
            {"рпед", "Эр Пед"},
            {"тсф", "Тэ Эс Эф"},
            {"срт", "Эс Эр Тэ"},
            {"обр", "О Бэ Эр"},
            {"кпк", "Кэ Пэ Ка"},
            {"пда", "Пэ Дэ А"},
            {"id", "Ай Ди"},
            {"мщ", "Эм Ще"},
            {"вт", "Вэ Тэ"},
            {"wt", "Вэ Тэ"},
            {"ерп", "Йе Эр Пэ"},
            {"се", "Эс Йе"},
            {"апц", "А Пэ Цэ"},
            {"лкп", "Эл Ка Пэ"},
            {"см", "Эс Эм"},
            {"ека", "Йе Ка"},
            {"ка", "Кэ А"},
            {"бса", "Бэ Эс Аа"},
            {"тк", "Тэ Ка"},
            {"бфл", "Бэ Эф Эл"},
            {"бщ", "Бэ Щэ"},
            {"кк", "Кэ Ка"},
            {"ск", "Эс Ка"},
            {"зк", "Зэ Ка"},
            {"ерт", "Йе Эр Тэ"},
            {"вкд", "Вэ Ка Дэ"},
            {"нтр", "Эн Тэ Эр"},
            {"пнт", "Пэ Эн Тэ"},
            {"авд", "А Вэ Дэ"},
            {"пнв", "Пэ Эн Вэ"},
            {"ссд", "Эс Эс Дэ"},
            {"крс", "Ка Эр Эс"},
            {"кпб", "Кэ Пэ Бэ"},
            {"кпс", "Кэ Пэ Эс"},
            {"сссп", "Эс Эс Эс Пэ"},
            {"крб", "Ка Эр Бэ"},
            {"бд", "Бэ Дэ"},
            {"сст", "Эс Эс Тэ"},
            {"скс", "Эс Ка Эс"},
            {"икн", "И Ка Эн"},
            {"нсс", "Эн Эс Эс"},
            {"емп", "Йе Эм Пэ"},
            {"бс", "Бэ Эс"},
            {"цкс", "Цэ Ка Эс"},
            {"срд", "Эс Эр Дэ"},
            {"жпс", "Джи Пи Эс"},
            {"gps", "Джи Пи Эс"},
            {"ннксс", "Эн Эн Ка Эс Эс"},
            {"ss", "Эс Эс"},
            {"тесла", "тэсла"},
            {"трейзен", "трэйзэн"},
            {"нанотрейзен", "нанотрэйзэн"},
            {"рпзд", "Эр Пэ Зэ Дэ"},
            {"кз", "Кэ Зэ"},
            {"рхбз", "Эр Хэ Бэ Зэ"},
            {"рхбзз", "Эр Хэ Бэ Зэ Зэ"},
            {"днк", "Дэ Эн Ка"},
            {"мк", "Эм Ка"},
            {"mk", "Эм Ка"},
            {"рпг", "Эр Пэ Гэ"},
            {"с4", "Си 4"}, // cyrillic
            {"c4", "Си 4"}, // latinic
            {"бсс", "Бэ Эс Эс"},
            {"сии", "Эс И И"},
            {"ии", "И И"},
            {"опз", "О Пэ Зэ"},
            {"ja", "Я"},
            {"mein", "Майн"},
            {"gott", "Готх"},
            {"nein", "Найнъ"},
            {"scheisse", "Шайз"},
            {"wurst", "Вуст"},
            {"wurste", "Вёстэ"},
            {"manner", "Мяннэ"},
            {"frauen", "Фраун"},
            {"herr", "Герр"},
            {"herren", "Геррен"},
            {"meine", "Майнэ"},
            {"hier", "Хие"},
            {"dumnkopf", "Думкопф"},
            {"dummköpfe", "Думкёпфэ"},
            {"schmetterling", "Шмэттерлин"},
            {"maschine", "Машинэ"},
            {"maschinen", "Машинэн"},
            {"achtung", "Ахтунг"},
            {"musik", "Музык"},
            {"kapitän", "Капитэин"},
            {"döner", "Дёнэр"},
            {"dankeschön", "Данке Шён"},
            {"gesundheit", "Гесундхайт"},
            {"flammenwerfer", "Фламэнверфер"},
            {"poltergeist", "Полтэргайст"},
            {"branntwein", "Брантвайн"},
            {"rucksack", "Рюксак"},
            {"medizin", "Медицин"},
            {"akzent", "Акцэнт"},
            {"anomalie", "Аномалий"},
            {"doof", "Доф"},
            {"warnung" , "Варнун"},
            {"wunderbar", "Вундэрбар"},
            {"warnungen", "Варнунгэ"},
            {"karpfen", "Карпфн"},
            {"bier", "Биэ"},
            {"guten", "Гутн"},
            {"krankenwagen", "Кранкн Вагн"},
            {"auf", "Ау"},
            {"wiedersehen", "Фидерзеин"},
            {"tschüss", "Чус"},
            {"tschau", "Чау"},
            {"fantastisch", "Фантастиш"},
            {"doppelgänger", "Доппэльгнгэа"},
            {"verboten", "Вэрботн"},
            {"schnell", "Шнэль"},
            {"krankenhaus", "Кранкнхауз"},
            {"kugelblitz", "Кьюгельблиц"},
            {"ist", "Ыст"},
        };

    private static readonly IReadOnlyDictionary<string, string> ReverseTranslit =
        new Dictionary<string, string>()
        {
            {"a", "а"},
            {"b", "б"},
            {"v", "в"},
            {"g", "г"},
            {"d", "д"},
            {"e", "е"},
            {"je", "ё"},
            {"zh", "ж"},
            {"z", "з"},
            {"i", "и"},
            {"y", "й"},
            {"k", "к"},
            {"l", "л"},
            {"m", "м"},
            {"n", "н"},
            {"o", "о"},
            {"p", "п"},
            {"r", "р"},
            {"s", "с"},
            {"t", "т"},
            {"u", "у"},
            {"f", "ф"},
            {"h", "х"},
            {"c", "ц"},
            {"x", "кс"},
            {"ch", "ч"},
            {"sh", "ш"},
            {"jsh", "щ"},
            {"hh", "ъ"},
            {"ih", "ы"},
            {"jh", "ь"},
            {"eh", "э"},
            {"ju", "ю"},
            {"ja", "я"},
        };
}

// Source: https://codelab.ru/s/csharp/digits2phrase
public static class NumberConverter
{
    private static readonly string[] Frac20Male =
    {
        "", "один", "два", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Frac20Female =
    {
        "", "одна", "две", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Hunds =
    {
        "", "сто", "двести", "триста", "четыреста",
        "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
    };

    private static readonly string[] Tens =
    {
        "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят",
        "шестьдесят", "семьдесят", "восемьдесят", "девяносто"
    };

    public static string NumberToText(long value, bool male = true)
    {
        if (value >= (long) Math.Pow(10, 15))
            return String.Empty;

        if (value == 0)
            return "ноль";

        var str = new StringBuilder();

        if (value < 0)
        {
            str.Append("минус");
            value = -value;
        }

        value = AppendPeriod(value, 1000000000000, str, "триллион", "триллиона", "триллионов", true);
        value = AppendPeriod(value, 1000000000, str, "миллиард", "миллиарда", "миллиардов", true);
        value = AppendPeriod(value, 1000000, str, "миллион", "миллиона", "миллионов", true);
        value = AppendPeriod(value, 1000, str, "тысяча", "тысячи", "тысяч", false);

        var hundreds = (int) (value / 100);
        if (hundreds != 0)
            AppendWithSpace(str, Hunds[hundreds]);

        var less100 = (int) (value % 100);
        var frac20 = male ? Frac20Male : Frac20Female;
        if (less100 < 20)
            AppendWithSpace(str, frac20[less100]);
        else
        {
            var tens = less100 / 10;
            AppendWithSpace(str, Tens[tens]);
            var less10 = less100 % 10;
            if (less10 != 0)
                str.Append(" " + frac20[less100 % 10]);
        }

        return str.ToString();
    }

    private static void AppendWithSpace(StringBuilder stringBuilder, string str)
    {
        if (stringBuilder.Length > 0)
            stringBuilder.Append(" ");
        stringBuilder.Append(str);
    }

    private static long AppendPeriod(
        long value,
        long power,
        StringBuilder str,
        string declension1,
        string declension2,
        string declension5,
        bool male)
    {
        var thousands = (int) (value / power);
        if (thousands > 0)
        {
            AppendWithSpace(str, NumberToText(thousands, male, declension1, declension2, declension5));
            return value % power;
        }
        return value;
    }

    private static string NumberToText(
        long value,
        bool male,
        string valueDeclensionFor1,
        string valueDeclensionFor2,
        string valueDeclensionFor5)
    {
        return
            NumberToText(value, male)
            + " "
            + GetDeclension((int) (value % 10), valueDeclensionFor1, valueDeclensionFor2, valueDeclensionFor5);
    }

    private static string GetDeclension(int val, string one, string two, string five)
    {
        var t = (val % 100 > 20) ? val % 10 : val % 20;

        switch (t)
        {
            case 1:
                return one;
            case 2:
            case 3:
            case 4:
                return two;
            default:
                return five;
        }
    }
}
