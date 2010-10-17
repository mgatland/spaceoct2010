using System;

namespace SpaceOctopus
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (SpaceOctGame game = new SpaceOctGame())
            {
                game.Run();
            }
        }
    }
#endif
}

