using CommandLine;

namespace MAMEDownloader
{
    public class Options
    {
        [Option('v', "verbose", Default = true, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [Option('s', "source", Default = "https://archive.org/details/MAME_0.202_Software_List_ROMs_merged", HelpText = "URL source of the MAME collection")]
        public string URL { get; set; }

        [Option('t', "target", HelpText = "Target Folder to store downloads (requires ~60GB free disk space)")]
        public string Target { get; set; }

        [Option('r', "resume", HelpText = "Resume the download starting with the specified file")]
        public string ResumeFilename { get; set; }
    }
}
