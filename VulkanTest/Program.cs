using System;

namespace VulkanTest;

public static unsafe partial class Program
{
    public static void Main()
    {
        try
        {
            var testApp = new TestApp();
            testApp.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}