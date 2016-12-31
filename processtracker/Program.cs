using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Timers;

namespace processtracker
{
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
            Console.Clear();

            var procName = options.ProcessToTrack;
            var userType = options.UserName == "current" ? UserType.CurrentUser : UserType.AllUsers;
            var group = options.GroupByName;

            System.Diagnostics.Process[] processes = GetProcesses(userType, procName);


            int GBtoBytes = 1024 * 1024 * 1024;
            if (processes != null && processes.Length > 0)
            {
                Console.WriteLine("");
                Console.WriteLine("Total Processes: {0}", processes.Length);

                var compInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                Console.WriteLine("Total Memory:     {0:0.00} GB", (double)compInfo.TotalPhysicalMemory / GBtoBytes);
                Console.WriteLine("Available Memory: {0:0.00} GB", (double)compInfo.AvailablePhysicalMemory / GBtoBytes);
                Console.WriteLine("Total VM:         {0:0.00} GB", (double)compInfo.TotalVirtualMemory / GBtoBytes);
                Console.WriteLine("Available VM:     {0:0.00} GB", (double)compInfo.AvailableVirtualMemory / GBtoBytes);

                Console.WriteLine("");
                string pattern = "{0,-48}  {1,-16:0.00}  {2,-10}  {3,-8}  {4,-8}";
                Console.WriteLine(string.Format(pattern, "Name (pid)", "WrkSet (GB)", "#Thds", "%CPU", "CPU(s)"));
                Console.WriteLine("-----------------------------------------------------------------------------------------------------------");


                ICollection<ProcessInfo> processInfoColl = ProcessInfoGenerator.GenerateProcessInfo(processes, options.GroupByName, options.SortOption);
                foreach (var proc in processInfoColl)
                {
                    var processName = proc.GetNameString();
                    var cpuUsage = 0.0;
                    double workingSetGB = proc.WorkingSet;
                    workingSetGB /= GBtoBytes;
                    string cpuTime = proc.TotalProcessorTime.ToString(@"dd\.hh\:mm\:ss\.fff");

                    Console.WriteLine(string.Format(pattern, processName, workingSetGB, proc.NumThreads, cpuUsage, cpuTime));
                }
            }
            else
            {
                Console.WriteLine("No Processes to track");
            }
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            ++Counter;
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
