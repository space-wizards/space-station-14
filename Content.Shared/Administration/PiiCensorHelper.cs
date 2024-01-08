namespace Content.Shared.Administration;

public static class PiiCensorHelper
{
    //Yeets all the numbers and turns them to * but leaves the . and : so it still looks like an IP
    public static string CensorIP(string ipAddress)
    {
        if (ipAddress == null)
            return null!;
        var chars = ipAddress.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Char.IsDigit(chars[i]))
            {
                chars[i] = '*';
            }
        }
        return new string(chars);
    }

    //just does a complete replacement of the chars with *
    public static string CensorHwid(string hwid)
    {
        if (hwid == null)
            return null!;
        var len = hwid.Length;
        return new string('*', len);
    }
}

