using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Timers;

namespace processtracker
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryStatus
    {
        /// <summary>
        /// Specifies the size, in bytes, of the MEMORYSTATUS structure.
        /// Set this member to sizeof(MEMORYSTATUS) when passing it to the GlobalMemoryStatus function.
        /// </summary>
        public int Length;
        /// <summary>
        /// Specifies a number between zero and 100 that gives a general idea of current memory use, in which zero indicates no memory use and 100 indicates full memory use.
        /// </summary>
        public int MemoryLoad;
        /// <summary>
        /// Indicates the total number of bytes of physical memory.
        /// </summary>
        public int TotalPhys;
        /// <summary>
        /// Indicates the number of bytes of physical memory available.
        /// </summary>
        public int AvailPhys;
        /// <summary>
        /// Indicates the total number of bytes that can be stored in the paging file.
        /// This number does not represent the physical size of the paging file on disk.
        /// </summary>
        public int TotalPageFile;
        /// <summary>
        /// Indicates the number of bytes available in the paging file.
        /// </summary>
        public int AvailPageFile;
        /// <summary>
        /// Indicates the total number of bytes that can be described in the user mode portion of the virtual address space of the calling process.
        /// </summary>
        public int TotalVirtual;
        /// <summary>
        /// Indicates the number of bytes of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling process.
        /// </summary>
        public int AvailVirtual;

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GlobalMemoryStatus(ref MemoryStatus lpBuffer);

        public static string GetMemoryStatus()
        {
            var retValue = new StringBuilder();
            MemoryStatus ms = GlobalMemoryStatus();

            int BytesFromMB = 1024 * 1024;
            int BytesFromGB = BytesFromMB * 1024;

            retValue.AppendLine(string.Format("Memory Load {0} %", ms.MemoryLoad));
            retValue.AppendLine(string.Format("Total Phys  {0} MB", ms.TotalPhys / BytesFromMB));
            retValue.AppendLine(string.Format("Avail Phys  {0} MB", ms.AvailPhys / BytesFromMB));
            retValue.AppendLine(string.Format("Tota PFile  {0} GB", ms.TotalPageFile / BytesFromGB));
            retValue.AppendLine(string.Format("Avai PFile  {0} GB", ms.AvailPageFile / BytesFromGB));
            retValue.AppendLine(string.Format("Total Virt  {0} MB", ms.TotalVirtual / BytesFromMB));
            retValue.AppendLine(string.Format("Avail Virt  {0} MB", ms.AvailVirtual / BytesFromMB));
            return retValue.ToString();
        }

        public static MemoryStatus GlobalMemoryStatus()
        {
            MemoryStatus ms = new MemoryStatus();
            ms.Length = Marshal.SizeOf(ms);
            GlobalMemoryStatus(ref ms);
            return ms;
        }
        public static int GetMemoryLoad()
        {
            var ms = GlobalMemoryStatus();
            return ms.MemoryLoad;
        }
    }

    class Program
    {
        static void About()
        { }
        

        enum UserType
        {
            CurrentUser,
            AllUsers
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static string GetProcessOwner(System.Diagnostics.Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                return wi.Name;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }


        static System.Diagnostics.Process[] GetProcesses(UserType userType, string procName)
        {
            System.Diagnostics.Process[] processes = null;
            
            if (procName == null)
                processes = System.Diagnostics.Process.GetProcesses();
            else
                processes = System.Diagnostics.Process.GetProcessesByName(procName);
            
            if(userType == UserType.CurrentUser)
            {
                string currUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                var filteredProcess = processes.Where(proc => GetProcessOwner(proc) == currUserName);
                processes = filteredProcess.ToArray();
            }

            return processes;
        }


        static void GenerateOutput(Options options)
        {
            var procName = options.ProcessToTrack;
            var userType = options.UserName == "current" ? UserType.CurrentUser : UserType.AllUsers;
            var group = options.GroupByName;

            System.Diagnostics.Process[] processes = GetProcesses(userType, procName);
            
            if (processes != null && processes.Length > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Total Processes:  {0}", processes.Length);
                Console.WriteLine("");

                ICollection<ProcessInfo> processInfoColl = ProcessInfoGenerator.GenerateProcessInfo(processes, options);
                InfoPrinter printer = new InfoPrinter(options);
                printer.Print(processInfoColl);
            }
            else
            {
                Console.WriteLine("No Processes to track");
            }
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            ++Counter;
            Console.Clear();
            GenerateOutput(Options);

            if(Options.Loop != 0 && Counter >= Options.Loop)
            {
                Timer timer = source as Timer;
                timer.Enabled = false;
                timer.Stop();
            }
        }


        static Options Options = null;
        static int Counter = 0;

        
        static void Main(string[] args)
        {
            About();

            Options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(args, Options))
            {
                Environment.Exit(0);
            }
            
            //  no loop
            if (Options.Loop == -1)
            {
                GenerateOutput(Options);
            }
            else
            {
                Timer timer = new Timer(1000);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Start();

                if (Options.Loop > -1)
                {
                    if (Options.Loop == 0)
                        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
                    else
                        System.Threading.Thread.Sleep(1000 * Options.Loop);
                }
            }

            Console.WriteLine();
        }        
    }
}
