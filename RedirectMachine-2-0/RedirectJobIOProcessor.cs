using Gizmo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RedirectMachine_2_0
{
    internal class RedirectJobIOProcessor
    {
        public string Directory { get; set; }
        public string InputOldUrlFile { get; set; }
        public string InputNewUrlFile { get; set; }
        public string InputExisting301File { get; set; }
        public string InputSubProjectFile { get; set; }
        public string LogFile { get; set; }
        public string OutputFolder { get; set; }
        public string OutputFoundUrlFile { get; set; }
        public string OutputLostUrlFile { get; set; }
        public string Output301CatchAllFile { get; set; }
        private string emailAddresses { get; set; }
        private List<string> logDump = new List<string>();
        //internal List<Tuple<string, string>> temp301s = new List<Tuple<string, string>>();
        //internal List<Tuple<string, string>> tempUrlHeaderMaps = new List<Tuple<string, string>>();

        public RedirectJobIOProcessor() { }

        /// <summary>
        /// working constructor
        /// </summary>
        /// <param name="directory"></param>
        public RedirectJobIOProcessor(string directory)
        {
            Directory = directory;

            if (System.IO.Directory.GetFiles(Directory, "*.xlsx").Length != 0)
            {
                Console.WriteLine("WARNING: Folder contains one or more xlsx files. Please change any xlsx file types to csv.");
                Gremlin.SendEmail("marcus.legault@scorpion.co", "xlsx files detected", "WARNING: Folder contains one or more xlsx files. Please change any xlsx file types to csv.");
            }

            InputOldUrlFile = System.IO.Directory.GetFiles(Directory, "OldSiteUrls.csv")[0];
            InputNewUrlFile = System.IO.Directory.GetFiles(Directory, "NewSiteUrls.csv")[0];
            InputExisting301File = System.IO.Directory.GetFiles(Directory, "Existing301s.csv")[0];
            if (File.Exists(Path.Combine(Directory, @"SubProjects.csv")))
                InputSubProjectFile = System.IO.Directory.GetFiles(Directory, "SubProjects.csv")[0];
            LogFile = Directory + @"\Log.txt";
            OutputFolder = Path.Combine(Directory, @"Output");
            OutputFoundUrlFile = Path.Combine(OutputFolder, @"FoundRedirects.csv");
            OutputLostUrlFile = Path.Combine(OutputFolder, @"LostRedirects.csv");
            Output301CatchAllFile = Path.Combine(OutputFolder, @"Possible301Catchalls.csv");

            checkForLog();
        }


        /// <summary>
        /// return found email addresses
        /// </summary>
        /// <returns></returns>
        internal string getEmailAddresses()
        {
            return emailAddresses;
        }

        /// <summary>
        /// check to see if a log exists. If it does, grab the first line out of the log file and check if it's an email address.
        /// if it's not an email address or an email wasn't found, use marcus.legault@scorpion.co
        /// delete file and add email address to log dump
        /// </summary>
        private void checkForLog()
        {
            string line = "";
            if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 0)
            {
                line = File.ReadLines(LogFile).First();
                File.Delete(LogFile);
            }

            emailAddresses = (line.Contains('@')) ? line : "marcus.legault@scorpion.co";
            addToLogDump(emailAddresses);

        }

        /// <summary>
        /// add line to logDump
        /// </summary>
        /// <param name="v"></param>
        internal void addToLogDump(string v)
        {
            logDump.Add(v);
        }

        /// <summary>
        /// create output directory
        /// </summary>
        internal void CreateOutputDirectory()
        {
            System.IO.Directory.CreateDirectory(@"" + OutputFolder);
        }

        /// <summary>
        /// write all information collected in dump collection to dump file
        /// </summary>
        internal void writeToLogDump()
        {
            using (StreamWriter fs = new StreamWriter(LogFile))
            {
                foreach (var line in logDump)
                {
                    fs.WriteLine(line);
                }
            }
        }

        internal List<Tuple<string, string>> ImportTuplesFromFile(string filePath)
        {
            List<Tuple<string, string>> tuples = new List<Tuple<string, string>>();
            using (var reader = new StreamReader(@"" + filePath))
            {
                while (!reader.EndOfStream)
                {
                    string[] tuple = reader.ReadLine().ToLower().Split(',');
                    if (tuple.Length >= 2)
                        tuples.Add(new Tuple<string, string>(tuple[0], tuple[1]));
                }
            }
            return tuples;
        }

        /// <summary>
        /// using the Existing301Redirects file, determine what lines are catchalls and what are redirected site maps
        /// if the line ends with a true bool or is null, add the line as a catchall redirect
        /// if the line ends with false, add the line as a headerMap tuple
        /// </summary>
        /// <param name="urlFile"></param>
        internal List<Tuple<string, string>> ImportExisting301sFromFile()
        {
            return ImportTuplesFromFile(InputExisting301File);
        }

        /// <summary>
        /// Add subproject contents to list
        /// </summary>
        /// <param name="urlFile"></param>
        internal List<Tuple<string, string>> ImportSubprojectsFromFile()
        {
            return (InputSubProjectFile != null) ? ImportTuplesFromFile(InputSubProjectFile) : new List<Tuple<string, string>>();
        }

        /// <summary>
        /// Add CSV file contents to list
        /// Sort results of list alphabetically
        /// </summary>
        /// <param name="urlFile"></param>
        internal List<Tuple<string, string>> ImportNewUrlsFromFile()
        {
            return ImportTuplesFromFile(InputNewUrlFile);
        }

        /// <summary>
        /// For Every line in CSV, read line and check if line belongs in a catchAll. If not, create new RedirectUrl Object.
        /// </summary>
        internal List<string> ImportOldUrlsFromFile()
        {
            List<string> urlList = new List<string>();
            using (var reader = new StreamReader(InputOldUrlFile))
            {
                while (!reader.EndOfStream)
                {
                    urlList.Add(reader.ReadLine().ToLower());
                }
            }
            return urlList;
        }

        /// <summary>
        /// export all urlDtos to CSVs. Determine if the urlDto is a found or lost url based on its score
        /// </summary>
        /// <param name="urlDtos"></param>
        internal void ExportNewCSVs(List<UrlDto> urlDtos)
        {
            List<string> foundList = new List<string>();
            List<string> lostList = new List<string>();
            int foundCount = 0;
            int lostCount = 0;

            foundList.Add("Old Site Url,Redirected Url,Flag");
            lostList.Add("Old Site Url, Potential Redirected Url");
            foreach (var urlDto in urlDtos)
            {
                if (urlDto.Score && !urlDto.Is301)
                {
                    foundCount++;
                    foundList.Add($"{urlDto.OriginalUrl},{urlDto.NewUrl}, {urlDto.Flag}");
                }
                else if (!urlDto.Is301)
                {
                    lostCount++;
                    if (urlDto.matchedUrls.Count > 0)
                    {

                        string[] arrayOfMatches = urlDto.matchedUrls.ToArray();
                        for (int i = 0; i < arrayOfMatches.Length; i++)
                        {
                            if (i == 0)
                                lostList.Add($"{urlDto.OriginalUrl},{arrayOfMatches[i]}");
                            else
                                lostList.Add($",{arrayOfMatches[i]}");
                        }
                    }
                    else
                        lostList.Add($"{urlDto.OriginalUrl}");
                }
            }
            ExportToCSV(foundList, OutputFoundUrlFile);
            ExportToCSV(lostList, OutputLostUrlFile);
            addToLogDump($"number of found urls: {foundCount}");
            addToLogDump($"number of lost urls: {lostCount}");
        }

        internal void export301CatchAllCSV(Existing301Utils existing301Utils)
        {
            ExportToCSV(existing301Utils.ExportCatchAllsToList(), Output301CatchAllFile);
            addToLogDump($"total number of catchalls found: {existing301Utils.CatchAllCount}");
        }

        /// <summary>
        /// build CSV from specified list of strings and export to specified filePath
        /// </summary>
        /// <param name="list"></param>
        /// <param name="filePath"></param>
        internal void ExportToCSV(List<string> list, string filePath)
        {
            using (TextWriter tw = new StreamWriter(@"" + filePath))
            {
                foreach (var item in list)
                {
                    tw.WriteLine(item);
                }
            }
        }
    }
}