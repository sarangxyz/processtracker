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

        static ICollection<ProcessInfo> GenerateProcessInfo_NoGroup(System.Diagnostics.Process[] processes)
        {
            ICollection<ProcessInfo> processInfoColl = new List<ProcessInfo>();
            foreach (var proc in processes)
            {
                var procInfo = new ProcessInfo();
                processInfoColl.Add(procInfo);
                PopulateProcInfo(procInfo, proc);
            }
            return processInfoColl;
        }

        static ICollection<ProcessInfo> GenerateProcessInfo_GroupByName(System.Diagnostics.Process[] processes)
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
            return processInfoCollDict.Values;
        }

        public static ICollection<ProcessInfo> GenerateProcessInfo(System.Diagnostics.Process[] processes, 
                                                                   bool groupByName,
                                                                   string sortOrder)
        {
            ICollection<ProcessInfo> processInfoColl = null;
            if (groupByName)
                processInfoColl = GenerateProcessInfo_GroupByName(processes);
            else
                processInfoColl = GenerateProcessInfo_NoGroup(processes);

            if (sortOrder != null)
            {
                ICollection<ProcessInfo> sorted = processInfoColl;
                if (sortOrder == "Name")
                {
                    sorted = processInfoColl.OrderBy<ProcessInfo, string>(item => item.Name).ToArray();
                }
                else if (sortOrder == "WrkSet")
                {
                    sorted = processInfoColl.OrderByDescending<ProcessInfo, long>(item => item.WorkingSet).ToArray();
                }

                processInfoColl = sorted;
            }

            return processInfoColl;
        }
    }
}
