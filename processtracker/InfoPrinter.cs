using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace processtracker
{
    internal class InfoPrinter
    {
        private Options _options = null;
        private string _pattern = string.Empty;
        public InfoPrinter(Options options)
        {
            _options = options;
            if (!_options.CmdLine)
                _pattern = "{0,-32}  {1,-6}  {2,-16:0.00}  {3,-10}  {4,-8}  {5,-16}    {6,-32}";
            else
                _pattern = "{0,-32}  {1,-6}  {2,-16:0.00}  {3,-10}  {4,-8}  {5,-16}";
        }

        private string GetPattern()
        {
            return _pattern;
        }

        private void PrintHeadLine()
        {
            if (!_options.CmdLine)
            {
                Console.WriteLine(string.Format(GetPattern(), "Name", "pid", "WrkSet (MB)", "#Thds", "%CPU", "CPU(s)", "CmdLine"));
                Console.WriteLine("---------------------------------------------------------------------------------------------------------------------------------------");
            }
            else
            {
                Console.WriteLine(string.Format(GetPattern(), "Name", "pid", "WrkSet (MB)", "#Thds", "%CPU", "CPU(s)"));
                Console.WriteLine("-----------------------------------------------------------------------------------------------------");
            }           
        }

        private void PrintProcessInfo(ProcessInfo proc)
        {
            int GBtoBytes = 1024 * 1024;
            var processName = proc.Name.Length < 33 ? proc.Name : proc.Name.Substring(0, 32);
            var idStr = proc.IsCollection() ? "#" + proc.NumInstances : proc.Id.ToString();
            var cpuUsage = 0.0;
            double workingSetMB = proc.WorkingSet;
            workingSetMB /= GBtoBytes;
            string cpuTime = proc.TotalProcessorTime.ToString(@"dd\.hh\:mm\:ss\.fff");

            var cmdLine = string.Empty;
            if (_options.CmdLine)
                cmdLine = proc.CommandLine;
            else
                cmdLine = proc.CommandLine.Length < 33 ? proc.CommandLine : proc.CommandLine.Substring(0, 32);

            if (!_options.CmdLine)
            {
                Console.WriteLine(string.Format(GetPattern(), processName, idStr, workingSetMB.ToString("N"), proc.NumThreads, cpuUsage, cpuTime, cmdLine));
            }
            else
            {
                Console.WriteLine(string.Format(GetPattern(), processName, idStr, workingSetMB.ToString("N"), proc.NumThreads, cpuUsage, cpuTime));
                Console.WriteLine("CommandLine: {0}", cmdLine);
                Console.WriteLine();
            }
        }

        public void Print(ICollection<ProcessInfo> processInfoColl)
        {
            PrintHeadLine();
            foreach (var proc in processInfoColl)
                PrintProcessInfo(proc);
        }
    }

}
