namespace IDontCare;

internal static class Program
{
    private static void Main(string[] args)
    {
        var name = args.Length > 0 ? string.Join(' ', args) : "world";
        Console.WriteLine($"Hello, {name}! Welcome to the i_dont_care sample project.");
    }
}
