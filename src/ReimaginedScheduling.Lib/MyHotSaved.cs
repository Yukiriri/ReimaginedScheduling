using System.Text.RegularExpressions;

namespace ReimaginedScheduling.Lib;

public static class MyHotSaved
{
    private static readonly string fileName = $"{new Regex(@"(?!\"").*(?=.(exe|dll))").Match(Environment.GetCommandLineArgs()[0]).Value}.save.txt";
    private static readonly FileInfo fileInfo = new(fileName);
    private static DateTime lastWriteTime;

    static MyHotSaved()
    {
        if (!fileInfo.Exists)
        {
            fileInfo.Create().Close();
        }
    }

    public static bool isChanged()
    {
        fileInfo.Refresh();
        return lastWriteTime != fileInfo.LastWriteTime;
    }
    
    public static string reload()
    {
        lastWriteTime = fileInfo.LastWriteTime;
        using var stream = fileInfo.OpenText();
        return stream.ReadToEnd();
    }
    
}
