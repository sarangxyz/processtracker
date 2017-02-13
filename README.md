# processtracker

A handy utility to list the running processes. Dumps out tabular output listing duplicate processes with Workset size.

```
Total Processes:  63

Name                              pid     WrkSet (MB)       #Thds       %CPU      CPU(s)              CmdLine
---------------------------------------------------------------------------------------------------------------------------------------
chrome                            #10     686.53            148         0         00.00:00:32.011
opera                             #8      533.53            110         0         00.00:00:25.084
devenv                            4356    416.03            67          0         00.00:03:50.522     devenv.exe  /debugexe D:\workdir
OUTLOOK                           9608    314.58            54          0         00.00:03:52.878     "C:\MSOffice\Office15\OUTLO
perl                              #2      236.61            7           0         00.00:06:47.786
lync                              5552    180.02            58          0         00.00:00:56.066     "C:\MSOffice\Office15\lync.
EXCEL                             11440   146.20            33          0         00.00:00:26.083     "C:\MSOffice\Office15\EXCEL
Microsoft.VsHub.Server.HttpHost   7964    139.02            79          0         00.00:00:07.940     "C:\Program Files (x86)\Common F
java                              9064    106.81            38          0         00.00:00:01.669     "C:\Java\jre8x64\bin\java"
explorer                          3152    103.50            29          0         00.00:01:42.430     C:\windows\Explorer.EXE
```

in case you want to check the cmd line parameters for a certain process execute following command:

```
c:\>procs  -c 1

Total Processes:  63

Name                              pid     WrkSet (MB)       #Thds       %CPU      CPU(s)
-----------------------------------------------------------------------------------------------------
chrome                            #10     686.23            133         0         00.00:00:36.395
CommandLine:

OUTLOOK                           9608    314.53            53          0         00.00:03:52.893
CommandLine: "C:\MSOffice\Office15\OUTLOOK.EXE"

perl                              #2      234.64            7           0         00.00:08:06.738
CommandLine:

lync                              5552    179.98            57          0         00.00:00:56.082
CommandLine: "C:\MSOffice\Office15\lync.exe" /fromrunkey

EXCEL                             11440   146.20            33          0         00.00:00:26.083
CommandLine: "C:\MSOffice\Office15\EXCEL.EXE"
```

in case you want detailed info about all running instances of a certain process command would be.
(-t is for threshold here, show any process with WrkSet greater than 10 MB and -c is for showing command line):

(default threshold value is 100MB):


```
c:\>procs -p notepad2 -t 10 -c 1

Total Processes:  3

Name                              pid     WrkSet (MB)       #Thds       %CPU      CPU(s)
-----------------------------------------------------------------------------------------------------
Notepad2                          10800   20.64             1           0         00.00:00:12.417
CommandLine: "C:\Program Files\Notepad2\Notepad2.exe" C:\logs\3e3550e428.log

Notepad2                          11836   16.28             1           0         00.00:00:05.834
CommandLine: "C:\Program Files\Notepad2\Notepad2.exe" C:\logs\3e388c587c.log

Notepad2                          12872   10.62             1           0         00.00:00:00.780
CommandLine: "C:\Program Files\Notepad2\Notepad2.exe" /z "C:\windows\system32\notepad.exe"
```

