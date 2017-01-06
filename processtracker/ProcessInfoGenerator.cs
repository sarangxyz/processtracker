using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace processtracker
{
    class ProcessInfoGenerator
    {
        protected static System.Diagnostics.PerformanceCounter CpuCounter = null;
        
        static void PopulateProcInfo(ProcessInfo procInfo, System.Diagnostics.Process proc)
        {
            procInfo.Name = proc.ProcessName;
            procInfo.Id = proc.Id;

            try
            {
                procInfo.TotalProcessorTime = proc.TotalProcessorTime;
                procInfo.WorkingSet = proc.WorkingSet64;
                procInfo.NumThreads = proc.Threads.Count;
            }
            catch (System.ComponentModel.Win32Exception)
            { }
        }

        static void AddProcStats(ProcessInfo procInfo, System.Diagnostics.Process proc)
        {
            procInfo.Id = 0;
            ++procInfo.NumInstances;

            try
            {
                procInfo.TotalProcessorTime += proc.TotalProcessorTime;
                procInfo.WorkingSet += proc.WorkingSet64;
                procInfo.NumThreads += proc.Threads.Count;
            }
            catch (System.ComponentModel.Win32Exception)
            { }
        }

        static List<ProcessInfo> GenerateProcessInfo_NoGroup(System.Diagnostics.Process[] processes)
        {
            List<ProcessInfo> processInfoColl = new List<ProcessInfo>();
            foreach (var proc in processes)
            {
                var procInfo = new ProcessInfo();
                processInfoColl.Add(procInfo);
                PopulateProcInfo(procInfo, proc);
            }
            return processInfoColl;
        }

        static List<ProcessInfo> GenerateProcessInfo_GroupByName(System.Diagnostics.Process[] processes)
        {
            var processInfoCollDict = new Dictionary<string, ProcessInfo>();
            foreach (var proc in processes)
            {
                if (processInfoCollDict.ContainsKey(proc.ProcessName))
                {
                    var procInfo = processInfoCollDict[proc.ProcessName];
                    AddProcStats(procInfo, proc);
                }
                else
                {
                    var procInfo = new ProcessInfo();
                    processInfoCollDict.Add(proc.ProcessName, procInfo);
                    PopulateProcInfo(procInfo, proc);
                }                
            }
            return processInfoCollDict.Values.ToList();
        }

        public static ICollection<ProcessInfo> GenerateProcessInfo(System.Diagnostics.Process[] processes, Options options)
        {
            List<ProcessInfo> processInfoColl = null;
            if (options.GroupByName)
                processInfoColl = GenerateProcessInfo_GroupByName(processes);
            else
                processInfoColl = GenerateProcessInfo_NoGroup(processes);

            //  purge all those below Threshold memory usage
            if(options.Threshold > 0)
                processInfoColl.RemoveAll(x => x.WorkingSet < options.Threshold * 1024 * 1024);


            if (options.SortOption != null)
            {
                ICollection<ProcessInfo> sorted = processInfoColl;
                if (options.SortOption == "Name")
                {
                    sorted = processInfoColl.OrderBy<ProcessInfo, string>(item => item.Name).ToArray();
                }
                else if (options.SortOption == "WrkSet")
                {
                    sorted = processInfoColl.OrderByDescending<ProcessInfo, long>(item => item.WorkingSet).ToArray();
                }

                processInfoColl = sorted.ToList();
            }

            return processInfoColl;
        }
    }
}
