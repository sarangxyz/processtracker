using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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

            foreach(var proc in processInfoColl)
                UpdateCommandLineForProcess(proc);

            return processInfoColl;
        }

        #region CMD_LINE
        //  credits:
        //  http://stackoverflow.com/a/16142791
        //

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, ref PROCESS_BASIC_INFORMATION ProcessInformation, int ProcessInformationLength, IntPtr ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref IntPtr lpBuffer, IntPtr dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref UNICODE_STRING lpBuffer, IntPtr dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.LPWStr)] string lpBuffer, IntPtr dwSize, IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);


        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public UIntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;

            public int Size
            {
                get { return (int)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)); }
            }
        }
        

        private const int PROCESS_QUERY_INFORMATION = 0x400;
        private const int PROCESS_VM_READ = 0x10;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct UNICODE_STRING
        {
            public short Length;
            public short MaximumLength;
            public IntPtr Buffer;
        }


        static void UpdateCommandLineForProcess(ProcessInfo proc)
        {
            if (proc.IsCollection())
                return;

            PROCESS_BASIC_INFORMATION basicInfo = new PROCESS_BASIC_INFORMATION();
            proc.CommandLine = basicInfo.Size.ToString();

            int processParametersOffset = 0x20;
            int offset = 0x70;
            var procHandle = OpenProcess((int)(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead), false, proc.Id);
            if(procHandle != IntPtr.Zero)
            {
                try
                {
                    int ProcessBasicInformation = 0x0;
                    //int status = NtQueryInformationProcess(procHandle, ProcessBasicInformation, ref basicInfo, basicInfo.Size, IntPtr.Zero);
                    PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                    int hr = NtQueryInformationProcess(procHandle, ProcessBasicInformation, ref pbi, Marshal.SizeOf(pbi), IntPtr.Zero);
                    if (hr != 0)
                        return;


                    IntPtr pp = new IntPtr();
                    if (!ReadProcessMemory(procHandle, pbi.PebBaseAddress + processParametersOffset, ref pp, new IntPtr(Marshal.SizeOf(pp)), IntPtr.Zero))
                        return;

                    UNICODE_STRING us = new UNICODE_STRING();
                    if (!ReadProcessMemory(procHandle, pp + offset, ref us, new IntPtr(Marshal.SizeOf(us)), IntPtr.Zero))
                        return;

                    if ((us.Buffer == IntPtr.Zero) || (us.Length == 0))
                        return;

                    string s = new string('\0', us.Length / 2);
                    if (!ReadProcessMemory(procHandle, us.Buffer, s, new IntPtr(us.Length), IntPtr.Zero))
                        return;

                    proc.CommandLine = s;
                }
                finally
                {
                    CloseHandle(procHandle);
                }
            }
        }

        #endregion

    }
}
