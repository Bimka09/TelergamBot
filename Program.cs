using System;

namespace Project
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var bot = new ThisBot())
            {
                bot.Start();
                Console.ReadLine();
            }
        }
    }
}
