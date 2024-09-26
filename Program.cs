using buffer;
internal class Program
{
    private static void Main(string[] args)
    {
        LineStarts lineStarts = new LineStarts();
        var name = "我的世界\r说听养狗\n不是好事\r\nubhida";
        var res = lineStarts.createLineStartsFast(name);
        foreach(var line in res){
            Console.WriteLine(line);
        }

    }
}