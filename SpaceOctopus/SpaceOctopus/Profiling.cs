#if PROFILING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.StopWatch;

namespace SpaceOctopus
{

    //from http://blogs.msdn.com/b/shawnhar/archive/2009/07/07/profiling-with-stopwatch.aspx
    class Profiler
    {
        public static List<Profiler> AllProfilers = new List<Profiler>();

        string name;
        double elapsedTime;
        Stopwatch stopwatch;

        public Profiler(string name)
        {
            this.name = name;
            AllProfilers.Add(this);
        }

        public void Start()
        {
            stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            elapsedTime += stopwatch.Elapsed.TotalSeconds;
        }

        public void Print(double totalTime)
        {
            Trace.WriteLine(string.Format("{0}: {1:F2}%", name, elapsedTime * 100 / totalTime));
            elapsedTime = 0;
        }
    }

   class ProfilerComponent : GameComponent
    {
        double totalTime;

        public ProfilerComponent(Game game)
            : base(game)
        { }

        public override void Update(GameTime gameTime)
        {
            totalTime += gameTime.ElapsedGameTime.TotalSeconds;

            if (totalTime >= 5)
            {
                foreach (Profiler profiler in Profiler.AllProfilers)
                    profiler.Print(totalTime);

                Trace.WriteLine(string.Empty);
                totalTime = 0;
            }
        }
    }



    /*
    struct ProfileMarker : IDisposable
    {
        public ProfileMarker(Profiler profiler)
        {
            this.profiler = profiler;
            profiler.Start();
        }

        public void Dispose()
        {
            profiler.Stop();
        }

        Profiler profiler;
    } 
     * 
     * allows you to go using (new ProfileMarker(drawProfiler))
     * {
     * ....
     * 
     * }
     */
}
#endif