using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RestSharp;
using Figgle;
using System.Linq;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(FiggleFonts.Standard.Render("URL Tester", null));
        Console.WriteLine("=================================================");
        Console.WriteLine("=        Fuzz URL List With WordList            =");
        Console.WriteLine("=              TG ID => @CSATM                  =");
        Console.WriteLine("=              Coded By iHes4m                  =");
        Console.WriteLine("= https://github.com/HesamTorkashvand/UrlTester =");
        Console.WriteLine("=================================================");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;

        string urlsFilePath;
        string wordsFilePath;
        string outputFilePath = "output.txt";

        try
        {
            if (File.Exists(outputFilePath))
            {
                Console.Write("[+] Output file already exists. Do you want to create a new list with the input files? [y/n]: ");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    Console.Write("[+] Enter file path for urls.txt: ");
                    urlsFilePath = Console.ReadLine();

                    Console.Write("[+] Enter file path for words.txt: ");
                    wordsFilePath = Console.ReadLine();

                    CreateOutputFile(urlsFilePath, wordsFilePath, outputFilePath);
                }
            }
            else
            {
                Console.Write("[+] Enter file path for urls.txt: ");
                urlsFilePath = Console.ReadLine();

                Console.Write("[+] Enter file path for words.txt: ");
                wordsFilePath = Console.ReadLine();

                CreateOutputFile(urlsFilePath, wordsFilePath, outputFilePath);
            }

            int urlCount = File.ReadLines(outputFilePath).Count();

            var checker = new UrlChecker(urlCount);
            checker.CheckUrls(outputFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private static void CreateOutputFile(string urlsFilePath, string wordsFilePath, string outputFilePath)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                string[] words = File.ReadAllLines(wordsFilePath);

                using (StreamReader reader = new StreamReader(urlsFilePath))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {

                        string baseUrl = line.TrimEnd('/');
                        foreach (string word in words)
                        {
                            string trimmedWord = word.Trim();
                            string requestUrl = baseUrl + "/" + trimmedWord;

                            writer.WriteLine(requestUrl);
                        }
                    }
                }
            }

            Console.WriteLine("[+] Output file created successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

}

class UrlChecker
{
    private readonly RestClient client;
    private readonly Regex titleRegex;
    private readonly Dictionary<string, List<string>> statusCodes;
    private int goodCount = 0;
    private int badCount = 0;
    private int errorCount = 0;
    private int remainingCount;

    public UrlChecker(int totalCount)
    {
        client = new RestClient();
        client.Timeout = 5000;
        titleRegex = new Regex(@"<title>(.*?)</title>");
        statusCodes = new Dictionary<string, List<string>>();
        remainingCount = totalCount;
    }

    public void CheckUrls(string outputFilePath)
    {
        try
        {
            string[] urls = File.ReadAllLines(outputFilePath);

            foreach (string url in urls)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(CheckUrl), url);
            }

            while (remainingCount > 0)
            {
                Thread.Sleep(1000);
            }

            WriteUrlsToFile(statusCodes, outputFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private void CheckUrl(object data)
    {
        string url = (string)data;

        try
        {
            var response = GetResponse(url);
            string statusCode = ((int)response.StatusCode).ToString();

            Match titleMatch = titleRegex.Match(response.Content);
            string pageTitle = "";
            if (titleMatch.Success)
                pageTitle = titleMatch.Groups[1].Value.Trim();

            AddUrlToStatusCodes(statusCodes, statusCode, url, pageTitle);

            WriteStatusCodeToFile(statusCode, url, pageTitle);

            PrintStatusAndPageTitle(statusCode, url, pageTitle);

            switch (statusCode)
            {
                case "200":
                    Interlocked.Increment(ref goodCount);
                    break;
                case "404":
                    Interlocked.Increment(ref badCount);
                    break;
                default:
                    Interlocked.Increment(ref errorCount);
                    break;
            }
            Interlocked.Decrement(ref remainingCount);
            Console.Title = $"Good: {goodCount} | Bad: {badCount} | Error: {errorCount} | Remaining: {remainingCount}";

            RemoveUrlFromOutputFile(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
            Interlocked.Decrement(ref remainingCount);
        }
    }

    private void AddUrlToStatusCodes(Dictionary<string, List<string>> statusCodes, string statusCode, string requestUrl, string pageTitle)
    {
        try
        {
            lock (statusCodes)
            {
                if (!statusCodes.ContainsKey(statusCode))
                    statusCodes.Add(statusCode, new List<string>());

                string urlWithTitle = requestUrl + " -- " + pageTitle;
                statusCodes[statusCode].Add(urlWithTitle);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private void WriteStatusCodeToFile(string statusCode, string requestUrl, string pageTitle)
    {
        try
        {
            string statusCodeFilePath = statusCode + ".txt";
            StreamWriter writer = new StreamWriter(statusCodeFilePath, true); // Set append mode to add new URLs to the end of the file
            string urlWithTitle = requestUrl + " -- " + pageTitle;
            writer.WriteLine(urlWithTitle);
            writer.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private void PrintStatusAndPageTitle(string statusCode, string requestUrl, string pageTitle)
    {
        try
        {
            ConsoleColor color = ConsoleColor.White;

            switch (statusCode)
            {
                case "200":
                    color = ConsoleColor.Green;
                    break;
                case "404":
                    color = ConsoleColor.Red;
                    break;
                case "500":
                    color = ConsoleColor.Yellow;
                    break;
            }

            Console.Write("[+] url => ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{requestUrl} | ");
            Console.ForegroundColor = color;
            Console.Write($"[ Status Code : {statusCode} ] -- ");
            Console.ResetColor();
            Console.WriteLine($" [ Title : {pageTitle} ] ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private void WriteUrlsToFile(Dictionary<string, List<string>> statusCodes, string outputFilePath)
    {
        try
        {
            foreach (KeyValuePair<string, List<string>> kvp in statusCodes)
            {
                string statusCode = kvp.Key;
                List<string> urls = kvp.Value;

                File.WriteAllLines(statusCode + ".txt", urls);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

    private int GetCountOfStatusCode(string statusCode, string targetStatusCode)
    {
        return statusCode == targetStatusCode && statusCodes.ContainsKey(statusCode) ? statusCodes[statusCode].Count : 0;
    }

    private IRestResponse GetResponse(string url)
    {
        var request = new RestRequest(url, Method.GET);
        IRestResponse response = null;

        try
        {
            response = client.Execute(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }

        return response;
    }

    private void RemoveUrlFromOutputFile(string url)
    {
        try
        {
            string outputFilePath = "output.txt";
            var file = new List<string>(System.IO.File.ReadAllLines(outputFilePath));
            file.Remove(url);
            System.IO.File.WriteAllLines(outputFilePath, file.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[-] Error: {ex.Message}");
        }
    }

}