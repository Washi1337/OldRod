using Carp.Core;

namespace Carp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var devirtualiser = new Devirtualiser(new ConsoleLogger());

            string filePath = args[0].Replace("\"", "");
            devirtualiser.Devirtualise(filePath);
        }
    }
}