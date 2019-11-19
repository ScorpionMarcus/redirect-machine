using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RedirectMachine_2_0
{
    internal class Existing301Utils
    {
        internal List<Tuple<string, string>> listOf301s;
        Dictionary<string, int> potentialCatchalls;
        List<string> existingCatchalls;

        internal int CatchAllCount = 0;

        /// <summary>
        /// default working constructor
        /// </summary>
        public Existing301Utils()
        {
            listOf301s = new List<Tuple<string, string>>();
            potentialCatchalls = new Dictionary<string, int>();
            existingCatchalls= new List<string>();
        }

        /// <summary>
        /// checks to see if the url belongs to one of the existing osParam catchAlls. If it does, don't do anything further with it. Essentially ignore that object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal bool checkExisting301Redirects(UrlDto urlDto)
        {
            string url = urlDto.OriginalUrl;
            // check to see if the url will be covered by one of the catchalls. If it is, ignore this url, up the CatchAllCount, and return true
            foreach(var catchAll in existingCatchalls)
            {
                if (url.StartsWith(catchAll))
                    return found301(urlDto);
            }
            // the Scorpion standard is to not redirect any specific urls that are using query strings. 
            // Because of this, ignore this url, up the CatchAllCount, and return true
            if (url.Contains("?"))
            {
                updatePotentialCatchAllList(url.Split('?')[0] + "?");
                return found301(urlDto);
            }
            // final check: make sure this url isn't already covered by the list of 301s imported from the Existing301s.
            // (this check verifies that there are no duplicates)
            // If it is, ignore this url, up the CatchAllCount, and return true.
            foreach (var tuple in listOf301s)
            {
                if (url.StartsWith(tuple.Item1))
                {
                    updatePotentialCatchAllList(url);
                    return found301(urlDto);
                }

            }
            // if all above checks fail, this is not a 301 redirect
            return false;
        }

        /// <summary>
        /// mini function which handles repetitive code used by several conditional checks
        /// </summary>
        /// <returns></returns>
        private bool found301(UrlDto urlDto)
        {
            CatchAllCount++;
            urlDto.Is301 = true;
            return true;
        }

        /// <summary>
        /// check to see if we already have the catchall
        /// </summary>
        /// <param name="url"></param>
        internal void updatePotentialCatchAllList(string url)
        {
            if (!potentialCatchalls.ContainsKey(url))
                potentialCatchalls.Add(url, 1);
            else
            {
                int count = potentialCatchalls[url] + 1;
                potentialCatchalls[url] = count;
            }
        }

        /// <summary>
        /// recursive method to check how many times each part of a url has been seen as a catchall
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="i"></param>
        internal void iterateThroughAllCatchAllsInUrl(string[] segments, int i)
        {
            if (i > 1)
            {
                string urlSub = String.Join("", segments, 0, i);
                updatePotentialCatchAllList(urlSub);
                i--;
                iterateThroughAllCatchAllsInUrl(segments, i);
            }
        }
         
        /// <summary>
        /// Sort catchAllList and then export catchAllList to CSV to specified filepath
        /// </summary>
        /// <param name="filePath"></param>
        public List<string> ExportCatchAllsToList()
        {
            List<string> exportListOf301s = new List<string>();
            exportListOf301s.Add("Potential 301 catchall redirect,Number of times seen,Notes");
            foreach (KeyValuePair<string, int> catchall in potentialCatchalls.OrderBy(key => key.Value))
            {

                if (catchall.Value > 3)
                {
                    if (catchall.Key.Contains("?"))
                        exportListOf301s.Add($"{catchall.Key},{catchall.Value},Query Parameter");
                    else
                        exportListOf301s.Add($"{catchall.Key},{catchall.Value}");
                }
            }
            exportListOf301s.Reverse(1, exportListOf301s.Count - 1);
            return exportListOf301s;
        }

        /// <summary>
        /// add catchall
        /// if url ends with '*', it is a catchall and add it to the existingCatchalls list.
        /// otherwise, add tuple to listOf301s
        /// </summary>
        /// <param name="tuple"></param>
        internal void AddNewCatchAllParam(Tuple<string, string> tuple)
        {
            if (tuple.Item1.EndsWith("*"))
                existingCatchalls.Add(tuple.Item1.Substring(0, tuple.Item1.Length - 1));
            else
                listOf301s.Add(tuple);
        }
    }
}