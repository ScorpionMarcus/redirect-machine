using Gizmo;
using System;
using System.Collections.Generic;
using System.IO;

namespace RedirectMachine_2_0
{
    internal class RedirectJobFinder
    {
        private string root;
        private List<RedirectJob> jobList = new List<RedirectJob>(); 

        public RedirectJobFinder(string root)
        {
            this.root = root;
        }

        internal void Run()
        {
            jobList = setUpJobs();
            if (jobList.Count > 0)
                Gremlin.Info($"Number of new jobs: {jobList.Count}");
            //Console.WriteLine($"number of jobs: {jobList.Count}");
            startJobs(jobList);
        }

        public int returnJobCount()
        {
            return jobList.Count;
        }


        private List<RedirectJob> setUpJobs()
        {
            List<RedirectJob> jobList = new List<RedirectJob>();
            string[] subdirectoryEntries = Directory.GetDirectories(root);
            foreach (string subDirectory in subdirectoryEntries)
            {
                if (checkCreationDate(subDirectory) && checkIfJobIsNeeded(subDirectory))
                    jobList.Add(new RedirectJob(subDirectory));
            }
            return jobList;
        }

        private bool checkCreationDate(string subDirectory)
        {
            DateTimeOffset creation = File.GetCreationTime(subDirectory);
            DateTimeOffset currentTime = DateTime.Now;
            //Console.WriteLine($"creation date: {creation}");
            //Console.WriteLine($"current time: {currentTime}");
            //Console.WriteLine($"how long ago the file was created: {currentTime.Subtract(creation).Days}");
            return true;
        }

        private static bool checkIfJobIsNeeded(string directory)
        {
            string[] subDirectories = Directory.GetDirectories(directory);
            return (subDirectories.Length < 1) ? true : false;
        }

        private void startJobs(List<RedirectJob> jobList)
        {
            string emailAddresses = "";
            foreach (var job in jobList)
            {
                job.Start();
                emailAddresses += job.EmailAddresses + ";";
            }
            if (jobList.Count > 0)
                Gremlin.SendEmail("marcus.legault@scorpion.co", $"There are {jobList.Count} new jobs.", $"Emails are being sent to {emailAddresses}");
        }
    }
}