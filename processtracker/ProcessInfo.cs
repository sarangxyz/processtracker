using System;
using System.Text;

namespace processtracker
{
    class ProcessInfo
    {
        public string Name { get; set; }
        public long WorkingSet { get; set; }
        public int NumThreads { get; set; }
        public double PercentCPU { get; set; }
        public int Id { get; set; }
        public int NumInstances { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public int PageFaults { get; set; }


        public ProcessInfo()
        {
            NumInstances = 1;
        }


        public bool IsCollection()
        {
            return NumInstances > 1;
        }

        public string GetNameString()
        {
            var bldr = new StringBuilder();
            bldr.Append(Name);
            bldr.Append(" ( ");
            if(IsCollection())
            {
                bldr.Append("#");
                bldr.Append(NumInstances);
            }
            else
            {
                bldr.Append(Id);
            }

            bldr.Append(" )");
            return bldr.ToString();
        }

        public override string ToString()
        {
            return GetNameString();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

}
