namespace piecetable;

public class Utils{
    public static bool startsWithUTF8BOM(string str)
    {
        return !!(str != null && str.Length>0 && str[0] == (int)CharCode.UTF8_BOM);
    }
}