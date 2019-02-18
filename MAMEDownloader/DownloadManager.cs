using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace MAMEDownloader
{
    public class DownloadManager
    {
        public string TargetFolder { get; set; }
        public string ResumeFilename { get; set; }
        public string URL { get; set; }

        public void DownloadFiles()
        {
            if (!Directory.Exists(TargetFolder))
                Directory.CreateDirectory(TargetFolder);

            var files = GetFilenames(URL);

            ulong requiredSpace = GetRequiredDiskSpace(files);
            ulong availableSpace = 0;
            if (DriveFreeBytes(TargetFolder, out availableSpace))
            {
                if (requiredSpace > availableSpace)
                {
                    Console.WriteLine("The download operation requires " + requiredSpace.ToString() + " but the target folder " + TargetFolder + " has only " + availableSpace.ToString() + " bytes available.");
                    return;
                }
            }

            Console.WriteLine("Downloading files to " + TargetFolder + ".");
            Console.WriteLine("Downloading " + files.Count().ToString() + " files from " + URL + " ...");
            if (!string.IsNullOrEmpty(ResumeFilename))
                Console.WriteLine("Resuming download session starting with " + ResumeFilename + " ...");

            DownloadBatchAsync(files);
        }

        public List<DownloadFile> GetFilenames(string url)
        {
            List<DownloadFile> downloads = new List<DownloadFile>();

            var web = new HtmlWeb();

            try
            {
                var doc = web.Load(url);
                var divsWithFormatFile = GetTagsWithClass(doc, new List<string>() { "format-file" });
                foreach (HtmlNode node in divsWithFormatFile)
                {
                    string downloadSize = node.InnerText;
                    var a = node.Descendants("a").FirstOrDefault();
                    string href = a.Attributes["href"].Value;
                    downloads.Add(new DownloadFile()
                    {
                        Size = ParseFileSize(downloadSize),
                        URL = "https://archive.org" + href,
                        Filename = GetFilenameFromURL(href)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return downloads;
        }

        public void DownloadBatchAsync(List<DownloadFile> downloads)
        {
            bool PastResumeFile = false;
            long fileCount = downloads.Count();
            ulong currentBytes = 0;
            ulong totalBytes = 0;
            foreach (var download in downloads)
            {
                totalBytes += download.Size;
            }

            int fileIndex = 1;
            using (var client = new WebClient())
            {
                foreach (DownloadFile download in downloads)
                {
                    // If resuming a download session, skip over all of the files until we get to 
                    // the file with which we want to resume the download session
                    if (!string.IsNullOrEmpty(ResumeFilename) && !PastResumeFile)
                    {
                        if (download.Filename != ResumeFilename)
                            continue;
                        PastResumeFile = true;
                    }
                    string targetFilename = Path.Combine(TargetFolder, download.Filename);
                    UriBuilder builder = new UriBuilder(download.URL);
                    Console.WriteLine(string.Format("{0:d}% complete - Downloading file # {1} of {2} -- {3}", ((currentBytes * 100) / totalBytes), fileIndex.ToString(), fileCount.ToString(), targetFilename));
                    client.DownloadFile(builder.Uri, targetFilename);
                    currentBytes += download.Size;
                    fileIndex++;
                }
                Console.WriteLine("100% complete!");
            }
        }

        public ulong GetRequiredDiskSpace(List<DownloadFile> downloads)
        {
            bool PastResumeFile = false;
            ulong totalBytes = 0;
            foreach (var download in downloads)
            {
                // If resuming a download session, skip over all of the files until we get to 
                // the file with which we want to resume the download session
                if (!string.IsNullOrEmpty(ResumeFilename) && !PastResumeFile)
                {
                    if (download.Filename != ResumeFilename)
                        continue;
                    PastResumeFile = true;
                }
                totalBytes += download.Size;
            }
            return totalBytes;
        }

        public static List<HtmlNode> GetTagsWithClass(HtmlDocument doc, List<string> @class)
        {
            var result = doc.DocumentNode.Descendants()
                .Where(x => x.Attributes.Contains("class") && @class.Contains(x.Attributes["class"].Value)).ToList();
            return result;
        }

        public ulong ParseFileSize(string size)
        {
            string sizeValue = size;
            int firstNewline = size.IndexOf('\n');
            int secondNewline = size.IndexOf('\n', firstNewline + 1);
            sizeValue = sizeValue.Substring(firstNewline + 1, secondNewline - 1);
            sizeValue = sizeValue.Trim();

            return GetBytesFromEstimate(sizeValue);
        }

        /// <summary>
        /// GetBytesFromEstimate will compute the number of bytes in a short hand
        /// file size estimate such as "3.14G" and return the rough number of bytes 
        /// represented.  
        /// 
        /// Supported units are:
        ///  G - Gigabyte (1073741824 bytes)
        ///  M - Megabyte (1048576 bytes)
        ///  K - Kilobyte (1024 bytes)
        ///    - Byte     (estimate is actual bytes)
        /// </summary>
        /// <param name="size">Size estimate with a decimal value follwed by a unit</param>
        /// <returns>long number of bytes</returns>
        private ulong GetBytesFromEstimate(string size)
        {
            string unit = size.Substring(size.Length - 1, 1);
            string sizeValue = size;
            ulong magnitude = 1;
            if (!Char.IsDigit(unit[0]))
            {
                sizeValue = size.Substring(0, size.Length - 1);
                switch (unit)
                {
                    case "G":
                        magnitude = 1073741824;
                        break;
                    case "M":
                        magnitude = 1048576;
                        break;
                    case "K":
                        magnitude = 1024;
                        break;
                }
            }
            decimal sizeEstimate = 0m;
            decimal.TryParse(sizeValue, out sizeEstimate);

            ulong bytes = (ulong)(sizeEstimate * magnitude);
            return bytes;
        }

        private static string GetFilenameFromURL(string URL)
        {
            string filename = URL;
            int forwardSlash = URL.LastIndexOf('/');
            if (-1 != forwardSlash)
            {
                filename = URL.Substring(forwardSlash + 1);
            }
            else
            {
                int backwardSlash = URL.LastIndexOf('\\');
                if (-1 != backwardSlash)
                    filename = URL.Substring(backwardSlash + 1);
            }
            return filename;
        }

        // Pinvoke for API function
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        public static bool DriveFreeBytes(string folderName, out ulong freespace)
        {
            freespace = 0;
            if (string.IsNullOrEmpty(folderName))
            {
                throw new ArgumentNullException("folderName");
            }

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            ulong free = 0, dummy1 = 0, dummy2 = 0;

            if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
            {
                freespace = free;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
