using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EV3.Transmitter
{
    public class Options
    {

        [Option('p', "prefix", Required = false, Default = "EV3", HelpText = "Try and connect to all bluetooth COM ports with the provided prefix in the device name. (Case insensitive)")]
        public string Prefix { get; set; }

        [Value(0, MetaName = "host", Required = true, HelpText = "The external host to expose the bricks too")]
        public string Host { get; set; }

        [Value(1, MetaName = "apikey", Required = true, HelpText = "The external host API Key")]
        public string ApiKey { get; set; }

    }
}
