using System;

namespace Engine.App;

public static partial class Program
{
    public static void Main()
    {
        // try
        // {
        Run<TestApp>();
        // Run<ClearScreenApp>();
        // Run<MultiWindowApp>();
        // Run<MultiInstanceWindowApp>();
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine(ex.ToString());
        //     throw;
        // }
    }

    private static void Run<TApp>() where TApp : Application, new()
    {
        Application app = new TApp();
        app.Run();
    }
}