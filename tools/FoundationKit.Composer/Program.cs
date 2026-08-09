namespace FoundationKit.Composer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await ComposerCli.RunAsync(
            args,
            Console.In,
            Console.Out,
            Console.Error,
            CancellationToken.None);
    }
}
