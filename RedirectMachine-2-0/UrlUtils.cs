using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedirectMachine_2_0
{
    public class UrlUtils
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public UrlUtils()
        {
        }

        /// <summary>
        /// actual working constructor
        /// </summary>
        /// <param name="originalUrl"></param>
        public UrlUtils(string originalUrl)
        {
        }

        /// <summary>
        /// split url into a temporary list
        /// eliminate blank entries from that list
        /// return the list as an array
        /// if the array is empty (such as the root directory), add an empty string
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal string[] SplitUrlChunks(string url)
        {
            List<string> tempList = url.Split(new Char[] { '-', '/' }).ToList();
            tempList.RemoveAll(i => i == "");
            if (!tempList.Any())
                tempList.Add("");
            return tempList.ToArray();
        }

        /// <summary>
        /// Return the urlResourceChunks[] array
        /// </summary>
        /// <returns></returns>
        internal string[] ReturnUrlChunks(string url)
        {
            return SplitUrlChunks(url);
        }

        /// <summary>
        /// Purpose of method: retrieve usable/searchable end of url from variable value.
        /// Get url text after last slash in url
        /// </summary>
        /// <param name="value"></param>
        public string TruncateString(string value)
        {
            string temp = CheckVars(value);
            int index = value.Length;
            int pos = temp.LastIndexOf("/") + 1;
            return temp.Substring(pos, temp.Length - pos);
        }

        /// <summary>
        /// Purpose of method: retrieve usable/searchable end of url from variable value.
        /// Get url text after last slash in url
        /// </summary>
        /// <param name="value"></param>
        public string BasicTruncateString(string value)
        {
            string temp = CheckVars(value.Substring(0, value.Length - 1));
            int pos = temp.LastIndexOf("/") + 1;
            int i = temp.LastIndexOf("/");
            return "/" + value.Substring(pos, value.Length - pos);
        }

        /// <summary>
        /// Purpose of method: retrieve usable/searchable end of url from variable value.
        /// Get url text after last slash in url,
        /// truncate temporary value to maxLength
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public string TruncateString(string value, int maxLength)
        {
            string temp = CheckVars(value);
            if (temp.EndsWith("/") && temp.Length > 1)
            {
                temp = temp.Substring(0, temp.Length - 1);
                int pos = temp.LastIndexOf("/") + 1;
                temp = temp.Substring(pos, temp.Length - pos);
                if (!temp.StartsWith("/"))
                    temp = "/" + temp;
            }
            return temp.Length <= maxLength ? temp : temp.Substring(0, maxLength);
        }

        /// <summary>
        /// Purpose: return first chunk of url. 
        /// Check if url starts with http or https. If it does, grab entire domain of url
        /// if that doesn't exist, return the first chunk of the url in between the first two '/'
        /// </summary>
        /// <param name="value"></param>
        public string TruncateStringHead(string value)
        {
            bool startsWithSlash = false;
            if (value.StartsWith("/"))
            {
                startsWithSlash = true;
                value = value.Substring(1);
            }
            else if (value.StartsWith("http"))
            {
                string[] segments = new Uri(value).Segments;
                value = "/" + ((segments.Length > 2) ? segments[1] : "");
                value = value.Substring(0, value.Length - 1);
            }
            int index = value.IndexOf("/");
            if (index <= -1)
                index = value.Length;

            value = value.Substring(0, index);
            if (startsWithSlash)
            {
                value = "/" + value;
                index++;
            }
            return value.Substring(0, index);
        }


        /// <summary>
        /// return if url contains header map
        /// </summary>
        /// <param name="urlHeaderMaps"></param>
        /// <returns></returns>
        internal string ReturnRemappedUrlParentDir(string url, List<Tuple<string, string>> urlHeaderMaps)
        {
            foreach (var tuple in urlHeaderMaps)
            {
                if (url.Contains(tuple.Item1))
                    return tuple.Item2;
            }
            return "/";
        }

        /// <summary>
        /// Purpose: return first chunk of url. 
        /// Check if url starts with http or https. If it does, grab entire domain of url
        /// if that doesn't exist, return the first chunk of the url in between the first two '/'
        /// </summary>
        /// <param name="value"></param>
        public string BasicTruncateStringHead(string value)
        {
            if (value.StartsWith("/"))
                value = value.Substring(1);
            int index = value.IndexOf("/");
            if (index <= -1)
                index = value.Length;
            return (!value.EndsWith("/")) ? "/" + value.Substring(0, index) + "/" : "/" + value.Substring(0, index);
        }

        /// <summary>
        /// remove unnecessary contents on end of url if found
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string CheckVars(string value)
        { 
            value = Regex.Replace(value, "--", "-");
            value = Regex.Replace(value, "---", "-");
            value = Regex.Replace(value, "dont", "don-t");
            value = Regex.Replace(value, "cant", "can-t");
            if (value.EndsWith(".aspx"))
                value = value.Substring(0, value.Length - 5);
            return value;
        } 

        /// <summary>
        /// return first position of j variable in string i.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static int GetFirstIndex(string i, string j)
        {
            return (i.Contains(j)) ? i.IndexOf(j) : i.Length;
        }

        /// <summary>
        /// return last position of j variable in string i. If not found, return all of string i
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public static int GetLastIndex(string i, string j)
        {
            return (i.Contains(j)) ? i.LastIndexOf(j) : i.Length;
        }

        /// <summary>
        /// return the substring of the string that is passed into this function.
        /// This method is overloaded with a bool. The bool indicates to the function that it must return a substring
        /// 1) if true, includes the string j rather than excluding it, or
        /// 2) if false, returns a substring that excludes string j.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string GetSubString(string i, string j, bool x)
        {
            int index = GetLastIndex(i, j);
            return (x == true) ? i.Substring(0, index + j.Length) : i.Substring(0, index);
        }

        /// <summary>
        /// return the substring of the string that is passed into this function.
        /// This method is overloaded with an int. The int indicates to the function that it must rerun that many times.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string GetSubString(string i, string j, int x)
        {
            var pos = 0;
            string temp = i;
            while (pos <= x)
            {
                int index = GetLastIndex(i, j);
                temp = temp.Substring(0, index);
                pos++;
            }
            return temp;
        }

        /// <summary>
        /// return a string build from a series of chunks from the working url
        /// </summary>
        /// <param name="index"></param>
        public string BuildChunk(string[] chunks, int index)
        {
            string temp = chunks[0];
            for (int i = 1; i < index; i++)
            {
                temp = temp + "-" + chunks[i];
            }
            return temp;
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
        /// do a very quick check if the url ends with a slash
        /// this also takes into account the root directory url.
        /// </summary>
        /// <param name="value"></param>
        private string CheckIfEndsWithSlash(string value)
        {
            if (value.Length == 1)
                return value;
            return value.EndsWith("/") ? value.Substring(0, value.Length - 1) : value;
        }
    }
}