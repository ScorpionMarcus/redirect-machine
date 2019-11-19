using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace RedirectMachine_2_0
{
    internal class RedirectMatcher
    {
        UrlUtils urlUtils; 

        /// <summary>
        /// default working constructor
        /// </summary>
        public RedirectMatcher()
        {
            urlUtils = new UrlUtils();
        }

        /// <summary>
        /// Start the finder program
        /// Initialize stopwatch.
        /// Import both the old urls and new urls into lists
        /// compare the new urls to the old urs using FindUrlMatches
        /// Export all found catchalls to a CSV file
        /// Export the lost url list and found url list to their respective CSVs
        /// Stop and display the recorded time on the stopwatch.
        /// </summary>
        internal void Run(List<UrlDto> urlDtos, List<Tuple<string, string>> newUrlSiteMap, Existing301Utils existing301Utils)
        {
            Console.WriteLine("begin search: "); 

            foreach (var urlDto in urlDtos)
            {
                if (!urlDto.OriginalUrl.Contains(".pdf") && !existing301Utils.checkExisting301Redirects(urlDto) && !findMatching301(urlDto, newUrlSiteMap))
                {
                    urlDto.Flag = "no match";
                    prepLostMatch(existing301Utils, urlDto.OriginalUrl);

                }
            }
            Console.WriteLine("end search");
        }

        internal void prepLostMatch(Existing301Utils existing301Utils, string url)
        {
            List<string> segments = new List<string>();
            Uri uri = new Uri((url.StartsWith("http")) ? url : "https://www.thisgoesnowhere.com" + url);
            segments = uri.Segments.ToList();
            if (uri.Host != "www.thisgoesnowhere.com")
                segments[0] = uri.GetLeftPart(UriPartial.Scheme) + uri.Host + "/";

            existing301Utils.iterateThroughAllCatchAllsInUrl(segments.ToArray(), segments.Count);

            //existing301Utils.updatePotentialCatchAllList(url);
            //existing301Utils.CatchAllCount++;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// return whether or not any of the finder functions were able to find the matching 301s
        /// </summary>
        /// <param name="urlDto"></param>
        /// <param name="newUrlSiteMap"></param>
        /// <returns></returns>
        internal bool findMatching301(UrlDto urlDto, List<Tuple<string, string>> newUrlSiteMap)
        {
            return (Basic301Finder(urlDto, newUrlSiteMap) || Advanced301Finder(urlDto, newUrlSiteMap) || ReverseAdvanced301Finder(urlDto, newUrlSiteMap) || Url301ChunkFinder(urlDto, newUrlSiteMap)) ? true : false;
        }

        /// <summary>
        /// scan every url in newUrlSiteMap list. if the resource directory of the url contains the object's UrlTail, add it to matched urls
        /// return the results of BasicScanMatchedUrls()
        /// set the flag to a value of 1
        /// </summary>
        /// <param name="newUrlSiteMap"></param>
        internal bool Basic301Finder(UrlDto urlDto, List<Tuple<string, string>> newUrlSiteMap)
        {
            urlDto.Flag = "Great Match";
            foreach (var url in newUrlSiteMap)
            {
                string resource = url.Item2;
                if (resource.Contains(urlDto.UrlResourceDir) && CheckParentAndResourceDirs(urlDto, url.Item1))
                    urlDto.matchedUrls.Add(url.Item1);
            }
            return (urlDto.matchedUrls.Count == 1) ? SetNewUrl(urlDto) : false;
        }

        /// <summary>
        /// scan every url in newUrlSiteMap. If the url's resource directory contains the first chunk from obj.urlChunks[], add it to a list of potential matches
        /// return whatever AdvancedScanMatchedUrls finds
        /// </summary>
        /// <param name="newUrlSiteMap"></param>
        internal bool Advanced301Finder(UrlDto urlDto, List<Tuple<string, string>> newUrlSiteMap)
        {
            urlDto.Flag = (urlDto.Flag == "Please Check Match") ? "Please Check Match" : "Good Match";
            urlDto.matchedUrls.Clear();
            var tupleList = new List<Tuple<string, int, int>>();
            foreach (var url in newUrlSiteMap)
            {
                string resource = url.Item2;
                string[] allOriginalUrlChunks = urlDto.UrlAllChunks;
                string[] originalUrlResourceChunks = urlDto.UrlResourceDirChunks;
                if (UrlMatchAnyChunks(url.Item2, originalUrlResourceChunks) && CheckParentAndResourceDirs(urlDto, url.Item1))
                {
                    if (!tupleList.Exists((Tuple<string, int, int> i) => i.Item1 == url.Item1))
                        tupleList.Add(new Tuple<string, int, int>(url.Item1, ReturnUrlMatches(url.Item1, allOriginalUrlChunks), ReturnUrlMatches(url.Item2, originalUrlResourceChunks) * 2));
                }
            }
            return AdvancedScanMatchedUrls(urlDto, tupleList);
        }

        /// <summary>
        /// create temporary tupleList to pass to other methods
        /// scan every url in newUrlSiteMap. 
        /// set string temp to the urlUtil resource directory value
        /// If temp contains the first chunk from urlUtil.urlResourceChunks[] and it passes the CheckParentAndResourceDirs() audit
        /// As long as the tupleList doesn't contain the url already (failsafe against duplicates)
        ///     set allUrlChunks[] array to take in the contents from the urlUtil.SplitUrlChunks(url) method. Note that the url being split is the newly scanned url
        ///     set urlResourceChunks[] array to take in the contents from the urlUtil.SplitUrlChunks() method by passing in the results of the BasicTruncateString(url) method. Note that the url being split is the newly scanned url
        ///     create a new tuple:
        ///         Item1 = potential url
        ///         Item2 = the number of matches the allUrlChunks[] entries were seen in the url
        /// return whatever AdvancedScanMatchedUrls finds
        /// </summary>
        /// <param name="newUrlSiteMap"></param>
        internal bool ReverseAdvanced301Finder(UrlDto urlDto, List<Tuple<string, string>> newUrlSiteMap)
        {
            urlDto.Flag = "Decent Match";
            urlDto.matchedUrls.Clear();
            var tupleList = new List<Tuple<string, int, int>>();
            foreach (var url in newUrlSiteMap)
            {
                string[] allNewUrlChunks = urlUtils.SplitUrlChunks(url.Item1);
                string[] newUrlResourceChunks = urlUtils.SplitUrlChunks(url.Item2);

                if (UrlMatchAnyChunks(urlDto.UrlResourceDir, newUrlResourceChunks) && CheckParentAndResourceDirs(urlDto, url.Item1))
                {
                    if (!tupleList.Exists((Tuple<string, int, int> i) => i.Item1 == url.Item1))
                        tupleList.Add(new Tuple<string, int, int>(url.Item1, urlUtils.ReturnUrlMatches(urlDto.OriginalUrl, allNewUrlChunks), urlUtils.ReturnUrlMatches(urlDto.OriginalUrl, newUrlResourceChunks) * 2));
                }
            }
            return AdvancedScanMatchedUrls(urlDto, tupleList);
        }

        /// <summary>
        /// Set initial int a (total matches in url) and b (double the total matches found in url resource dir) to 1
        /// for each item in the tuple list:
        /// if either the tuple's total url matches (Item2) are less than a OR total resource dir matches (Item3) are less than b, skip
        /// else if both Item2 > a AND Item3 > b, this is a much better match than all other previously found potential urls
        ///     call resetMatchedUrls() function
        ///     add the tuple's url (Item1) to the newly emptied matchedUrls list.
        ///     set a to the tuple's Item2 and b to the tuple's Item3
        /// else just add the tuple's Item1 to the matchedUrls
        /// check if the matchedUrls Count == 1. If true, url is found, setNewUrl, and return true
        ///     else, return false
        /// </summary>
        /// <param name="tupleList"></param>
        /// <returns></returns>
        private bool AdvancedScanMatchedUrls(UrlDto urlDto, List<Tuple<string, int, int>> tupleList)
        {
            int a = 1,
                b = 1;
            foreach (var item in tupleList)
            {
                if (item.Item2 < a || item.Item3 < b) { }
                else if (item.Item2 > a && item.Item3 > b)
                {
                    urlDto.matchedUrls.Clear();
                    urlDto.matchedUrls.Add(item.Item1);
                    a = item.Item2;
                    b = item.Item3;
                }
                else
                    urlDto.matchedUrls.Add(item.Item1);
            }
            return (urlDto.matchedUrls.Count == 1) ? SetNewUrl(urlDto) : false;
        }

        /// <summary>
        /// reset matchedUrls list to zero
        /// scan each url in the list of urls passed into the function
        /// Truncate the url and return the resource directory and assign that string to string temp
        /// if first chunk in the resource directory of the original url in question is contained in temp and the url passes the CheckParentAndResourceDirs() audit, add as a potential matched url
        /// return the ScanMatchedUrlsByChunk bool
        /// </summary>
        /// <param name="newUrlSiteMap"></param>
        /// <returns></returns>
        internal bool Url301ChunkFinder(UrlDto urlDto, List<Tuple<string, string>> newUrlSiteMap)
        {
            urlDto.Flag = "Please Check Match";
            urlDto.matchedUrls.Clear();
            List<Tuple<string, string>> possibleMatchList = new List<Tuple<string, string>>();
            foreach (var url in newUrlSiteMap)
            {
                string temp = url.Item2;
                string[] originalUrlResourceChunks = urlDto.UrlAllChunks;

                if (temp.Contains(originalUrlResourceChunks[0]) && CheckParentAndResourceDirs(urlDto, url.Item1))
                    possibleMatchList.Add(new Tuple<string, string>(url.Item1, url.Item2));
            }
            return ScanMatchedUrlsByChunk(urlDto, possibleMatchList);
        }

        /// <summary>
        /// Create two temporary lists.
        /// build string temp out of chunks from original url. for every iteration of the for loop, add a new chunk to the end of the url
        /// check if chunk is contained in each of the new urls. If it is, keep. If not, remove from passive list and run RemoveFromMatchedUrls method
        /// if Count == 0, obviously no matching url wasn't found. Return false
        /// if Count == 1, a single match has been found. Return true
        /// if Count > 1, run the for loop again to build a new substring of originalUrl
        /// once the entire resource dir has been built from the chunks and there are still more than one matched url, run the passive list through the AdvancedUrlFinder and return its result
        /// </summary>
        private bool ScanMatchedUrlsByChunk(UrlDto urlDto, List<Tuple<string, string>> possibleMatchList)
        {
            List<string> activeList = new List<string>();
            for (int i = 0; i < urlDto.UrlResourceDirChunks.Length; i++)
            {
                activeList.Clear();
                foreach (var tuple in possibleMatchList)
                {
                    activeList.Add(tuple.Item1);
                    urlDto.matchedUrls.Add(tuple.Item1);
                }
                string temp = urlUtils.BuildChunk(urlDto.UrlResourceDirChunks, i);
                foreach (var url in activeList)
                {
                    if (!url.Contains(temp))
                    {
                        possibleMatchList.RemoveAll(item => item.Item1 == url);
                        urlDto.matchedUrls.Remove(url);
                    }
                }
                if (urlDto.matchedUrls.Count == 0)
                    return false;
                if (urlDto.matchedUrls.Count == 1)
                    return CheckFinalUrlChunk(urlDto, i);
            }
            return Advanced301Finder(urlDto, possibleMatchList);
        }

        /// <summary>
        /// if at least one more larger substring can be built from the chunks of the original url, build that substring
        ///     check if that substring is contained in the last remaining matched url in the list
        ///     if contained, return true. Else, return false
        /// else, by default return true
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool CheckFinalUrlChunk(UrlDto urlDto, int i)
        {
            if (i < urlDto.UrlResourceDirChunks.Length)
            {
                string temp = urlUtils.BuildChunk(urlDto.UrlResourceDirChunks, i + 1);
                return (urlDto.matchedUrls.First().Contains(temp)) ? SetNewUrl(urlDto) : false;
            }
            else return true;
        }


        /// <summary>
        /// this method simply catches all urls that can immediately be removed from being evaluated.
        /// If there is a header map, does the url start with that header map? if yes, return true. If no, return false
        /// Else, if there's no header map, does the url's parent directory match the old url's parent directory? return either true or false
        /// </summary>
        /// <param name="urlDto"></param>
        /// <param name="url"></param>
        private bool CheckParentAndResourceDirs(UrlDto urlDto, string url)
        {
            if (!CheckResourceDirs(urlDto, url))
                return false;
            return url.StartsWith(urlDto.RemappedParentDir);
        }

        /// <summary>
        /// return whether or not the resource has a "." extension
        /// </summary>
        /// <param name="urlDto"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool CheckResourceDirs(UrlDto urlDto, string url)
        {
            if (urlDto.UrlResourceDir.Contains("."))
            {
                if (url.Contains("."))
                {
                    string x = urlDto.UrlResourceDir.Split('.')[1];
                    string y = url.Split('.')[1];
                    return (x == y) ? true : false;
                }
                else return false;
            }
            return true;
        }

        /// <summary>
        /// return if url contains any of the chunks
        /// </summary>
        /// <param name="url"></param>
        /// <param name="chunks"></param>
        /// <returns></returns>
        private bool UrlMatchAnyChunks(string url, string[] chunks)
        {
            foreach (var chunk in chunks)
            {
                if (url.Contains(chunk))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// return the number of times each entry in the chunks[] array is seen in string url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="chunks"></param>
        /// <returns></returns>
        internal int ReturnUrlMatches(string url, string[] chunks)
        {
            int j = 0;
            foreach (var chunk in chunks)
            {
                if (url.Contains(chunk))
                    j = j + (chunk.Length > 0 ? chunk.Length - 1 : 0);
            }
            return j;
        }

        /// <summary>
        /// set the new url to the only matched url left in matchedUrls list
        /// </summary>
        private bool SetNewUrl(UrlDto urlDto)
        {
            urlDto.NewUrl = urlDto.matchedUrls.First();
            urlDto.Score = true;
            return true;
        }
    }
}