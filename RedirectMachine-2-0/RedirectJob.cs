using Gizmo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RedirectMachine_2_0
{ 
    internal class RedirectJob
    {
        public string EmailAddresses { get; set; }
        internal RedirectJobIOProcessor jobIOProcessor;
        internal RedirectMatcher redirectMatcher;
        internal UrlUtils urlUtils;
        internal Existing301Utils existing301Utils;
        internal List<Tuple<string, string>> newUrlSiteMap;
        internal List<Tuple<string, string>> subProjectMaps;
        internal List<UrlDto> urlDtos;

        /// <summary>
        /// working constructor
        /// </summary>
        /// <param name="directory"></param>
        public RedirectJob(string directory)
        {
            urlUtils = new UrlUtils();
            existing301Utils = new Existing301Utils();
            newUrlSiteMap = new List<Tuple<string, string>>();
            jobIOProcessor = new RedirectJobIOProcessor(directory);
            redirectMatcher = new RedirectMatcher();
            subProjectMaps = new List<Tuple<string, string>>();
            urlDtos = new List<UrlDto>();
        }


        /// <summary>
        /// start redirect job methods
        /// </summary>
        public void Start()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            EmailAddresses = jobIOProcessor.getEmailAddresses();
            jobIOProcessor.addToLogDump($"job started at {DateTime.Now.ToString("MM/dd/yyyy HH:mm")}");
            jobIOProcessor.CreateOutputDirectory();
            Console.WriteLine("starting");
            importListsFromFiles();
            startRedirectFinder(); 
            exportListsToFiles();
            Console.WriteLine($"Sending email to {EmailAddresses}");
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = "Elapsed time: " + String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            jobIOProcessor.addToLogDump(elapsedTime);
            jobIOProcessor.writeToLogDump();

            Gremlin.SendEmail(EmailAddresses, $"Your redirect job for {Path.GetFileName(jobIOProcessor.Directory)} is done.", $"Your redirect job for {jobIOProcessor.Directory} is done. Please retrieve it within 24 hours");
        }

        private void exportListsToFiles()
        {
            jobIOProcessor.ExportNewCSVs(urlDtos);
            jobIOProcessor.export301CatchAllCSV(existing301Utils);
        }

        private void importListsFromFiles()
        {
            importExisting301s();
            subProjectMaps = jobIOProcessor.ImportSubprojectsFromFile();
            newUrlSiteMap = jobIOProcessor.ImportNewUrlsFromFile();
            createListOfDtos();
        }

        private void importExisting301s()
        {
            List<Tuple<string, string>> temp301s = jobIOProcessor.ImportExisting301sFromFile();
            foreach (var tuple in temp301s)
            {
                existing301Utils.AddNewCatchAllParam(tuple);
            }
        }

        private void createListOfDtos()
        {
            List<string> list = jobIOProcessor.ImportOldUrlsFromFile();
            foreach (var line in list)
            {
                urlDtos.Add(createUrlDto(line));
            }
        }

        private void startRedirectFinder()
        {
            redirectMatcher.Run(urlDtos, newUrlSiteMap, existing301Utils);
        }

        /// <summary>
        /// For Every line in CSV, read line and check if line belongs in a catchAll. If not, create new RedirectUrl Object.
        /// </summary>
        /// <param name="urlFile"></param>
        private void ImportOldUrlsIntoList(string urlFile)
        {
            using (var reader = new StreamReader(@"" + urlFile))
            {
                while (!reader.EndOfStream)
                {
                    string url = reader.ReadLine().ToLower();
                }
            }
            Console.WriteLine("Done importing old urls into list");
        }


        /// <summary>
        /// return a UrlDto object with all the props from UrlUtils class
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private UrlDto createUrlDto(string url)
        {
            UrlDto urlDto = new UrlDto(url);
            urlDto.UrlParentDir = urlUtils.TruncateStringHead(url);
            urlDto.UrlResourceDir = urlUtils.TruncateString(url, 48);
            urlDto.UrlResourceDirChunks = urlUtils.ReturnUrlChunks(urlDto.UrlResourceDir);
            urlDto.UrlAllChunks = urlUtils.ReturnUrlChunks(url);
            urlDto.RemappedParentDir = urlUtils.ReturnRemappedUrlParentDir(url, subProjectMaps);
            return urlDto;
        }
    }
}