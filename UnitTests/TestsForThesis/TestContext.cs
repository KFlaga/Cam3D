using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CamUnitTest.TestsForThesis
{
    public static class ModeCheck
    {
        public static bool IsTracking(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), true);
            if(attributes == null || attributes.Length == 0)
                return false;

            var d = (DebuggableAttribute)attributes[0];
            
            if(d.IsJITTrackingEnabled && d.IsJITOptimizerDisabled) { return true; }
            return false;
        }
    }

    public class Context
    {
        // TODO: Move shortVer parameter here

        public string ResultDirectory { get; set; } = "d:\\Cam3DTests";

        public string TestSuiteName { get; set; }
        public string TestName { get; set; }
        public string ResultPath { get; set; }
        public string Outcome { get; set; }

        public DateTime StartTime { get; set; }

        public StringBuilder Output { get; private set; }
        private StringBuilder results;

        public Context(string testName)
        {
            TestSuiteName = testName;
            var date = DateTime.Now;
            ResultPath = ResultDirectory + "\\" + TestSuiteName + "_" + 
                date.DayOfYear.ToString() + "_" + date.Hour.ToString("D2") + "_" + date.Minute.ToString("D2") + "_" + date.Second.ToString("D2") + 
                ".results";
            using(var outFile = new FileStream(ResultPath, FileMode.Create)) { }
        }

        public void InitTestSet(string testName)
        {
            TestName = testName;
            StartTime = DateTime.Now;
            results = new StringBuilder();
            Output = new StringBuilder();
        }

        public delegate void BenchmarkFunction();
        public void RunBenchmark(BenchmarkFunction function, int repeats = 1000, string description = "")
        {
            Output = new StringBuilder();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            for(int i = 0; i < repeats; ++i)
            {
                function();
            }
            timer.Stop();
            double totalMs = timer.Elapsed.TotalMilliseconds;

            results.AppendLine();
            results.AppendLine("Case: " + description);
            results.AppendLine("Total Time: " + GetTimeString(totalMs));
            results.AppendLine("Repeats: " + repeats);
            results.AppendLine("Per Time: " + GetTimeString(totalMs / repeats));
            if(Output.Length > 0)
            {
                results.AppendLine("Output:");
                results.AppendLine(Output.ToString());
            }
            Output.Clear();
        }

        public string GetTimeString(double ms)
        {
            if(ms > 1000.0) { return (ms / 1000).ToString("F3") + " [s]"; }
            if(ms > 0.01) { return ms.ToString("F3") + " [ms]"; }
            else { return (ms * 1000).ToString("F3") + " [us]"; }
        }

        public delegate bool TestFunction();
        public void RunTest(TestFunction function, string description = "")
        {
            if(Output.Length > 0)
            {
                results.AppendLine(Output.ToString());
            }

            Output = new StringBuilder();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            
            Exception ex = null;

            if(ModeCheck.IsTracking(Assembly.GetCallingAssembly()))
            {
                Outcome = function() ? "PASSED" : "FAILED";
            }
            else
            {
                try
                {
                    Outcome = function() ? "PASSED" : "FAILED";
                }
                catch(Exception e)
                {
                    Outcome = "ERROR";
                    ex = e;
                }
            }
            timer.Stop();
            double totalMs = timer.Elapsed.TotalMilliseconds;

            results.AppendLine();
            results.AppendLine("Case: " + description);
            results.AppendLine("Total Time: " + GetTimeString(totalMs));
            results.AppendLine("Outcome: " + Outcome);

            if(ex != null)
            {
                results.AppendLine("Error details:");
                results.AppendLine(ex.Message);
            }

            if(Output.Length > 0)
            {
                results.AppendLine("Output:");
                results.AppendLine(Output.ToString());
            }
            Output.Clear();
        }

        public void StoreTestBegin()
        {
            var outFile = new FileStream(ResultPath, FileMode.Append);
            using(TextWriter writer = new StreamWriter(outFile))
            {
                writer.WriteLine("===========================================================================");
                writer.WriteLine("Test Name: " + TestName);
                writer.WriteLine("Test Time: " + StartTime.ToString());
                writer.WriteLine("===========================================================================");
            }
            outFile.Close();
        }

        public void StoreTestOutput()
        {
            string text = results.ToString();
            results.Clear();

            var outFile = new FileStream(ResultPath, FileMode.Append);
            using(TextWriter writer = new StreamWriter(outFile))
            {
                writer.WriteLine(text);
            }
            outFile.Close();
        }
    }
}
