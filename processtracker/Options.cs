using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace processtracker
{
    class Options
    {
        [Option('u', "user", Required = false, DefaultValue = "current",
          HelpText = "List processes by user. Valid options are all/current. Defaults to current")]
        public string UserName { get; set; }

        [Option('v', "verbose", DefaultValue = false, Required = false,
          HelpText = "Prints all messages to standard output. Defaults to false")]
        public bool Verbose { get; set; }

        [Option('p', "process", Required = false,
          HelpText = "Prints information about provided application.")]
        public string ProcessToTrack { get; set; }

        [Option('s', "sort", Required = false, DefaultValue = "WrkSet",
          HelpText = "Specify option to sort. Valid options are Name/WrkSet")]
        public string SortOption { get; set; }

        [Option('g', "group", Required = false, DefaultValue = true,
          HelpText = "Specifies if the processes should be grouped")]
        public bool GroupByName { get; set; }

        [Option('l', "loop", Required = false, DefaultValue = -1,
          HelpText = "If data collection should be looped. Specify 0 for infinite looping or +ve number for #iterations")]
        public int Loop { get; set; }

        [Option('t', "threshold", Required = false, DefaultValue = 100,
          HelpText = "Threshold of Memory in MB below which processes are filtered")]
        public int Threshold { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
