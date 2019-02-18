using CommandLine;
using KnownFolderPaths;
using System.IO;

namespace MAMEDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            DownloadManager downloader = new DownloadManager();
            downloader.TargetFolder = Path.Combine(KnownFolders.GetPath(KnownFolder.Downloads), @"MAME\ROMS");
            downloader.URL = "https://archive.org/details/MAME_0.202_Software_List_ROMs_merged";

            // Parse the command line
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    downloader.ResumeFilename = o.ResumeFilename;

                    if (!string.IsNullOrEmpty(o.Target))
                        downloader.TargetFolder = o.Target;

                    if (!string.IsNullOrEmpty(o.URL))
                        downloader.URL = o.URL;

                    downloader.DownloadFiles();
                });
        }
    }
}
