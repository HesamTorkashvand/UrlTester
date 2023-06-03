using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RestSharp;
using Figgle;

class program
{
    static void Main(string[] args)
    {
        Console.Title = "Url Tester";
        Console.Clear();
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
        Console.Write("[+] Enter file path for urls.txt: ");
        string urlsFilePath = Console.ReadLine();
        Console.Write("[+] Enter file path for words.txt: ");
        string wordsFilePath = Console.ReadLine();
        var checker = new UrlChecker();
        checker.CheckUrls(urlsFilePath, wordsFilePath);
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

    public UrlChecker()
    {
        client = new RestClient();
        client.Timeout = 5000;
        titleRegex = new Regex(@"<title>(.*?)</title>");
        statusCodes = new Dictionary<string, List<string>>();
    }

    public void CheckUrls(string urlsFilePath, string wordsFilePath)
    {
        string[] words = File.ReadAllLines(wordsFilePath);

        Dictionary<string, List<string>> statusCodes = new Dictionary<string, List<string>>();

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
                    var response = GetResponse(requestUrl);
                    string statusCode = ((int)response.StatusCode).ToString();

                    Match titleMatch = titleRegex.Match(response.Content);
                    string pageTitle = "";
                    if (titleMatch.Success)
                        pageTitle = titleMatch.Groups[1].Value.Trim();

                    AddUrlToStatusCodes(statusCodes, statusCode, requestUrl, pageTitle);

                    WriteStatusCodeToFile(statusCode, requestUrl, pageTitle);

                    PrintStatusAndPageTitle(statusCode, requestUrl, pageTitle);

                    switch (statusCode)
                    {
                        case "200":
                            goodCount++;
                            break;
                        case "404":
                            badCount++;
                            break;
                        default:
                            errorCount++;
                            break;
                    }

                    Console.Title = $"Good: {goodCount} | Bad: {badCount} | Error: {errorCount}";
                }
              
            }

            WriteUrlsToFile(statusCodes);
        }
    }

    private void AddUrlToStatusCodes(Dictionary<string, List<string>> statusCodes, string statusCode, string requestUrl, string pageTitle)
    {
        if (!statusCodes.ContainsKey(statusCode))
            statusCodes.Add(statusCode, new List<string>());

        string urlWithTitle = requestUrl + " -- " + pageTitle;
        statusCodes[statusCode].Add(urlWithTitle);
    }

    private void WriteStatusCodeToFile(string statusCode, string requestUrl, string pageTitle)
    {
        string statusCodeFilePath = statusCode + ".txt";
        StreamWriter writer = new StreamWriter(statusCodeFilePath);
        string urlWithTitle = requestUrl + " -- " + pageTitle;
        writer.WriteLine(urlWithTitle);
        writer.Close();
    }

    private void PrintStatusAndPageTitle(string statusCode, string requestUrl, string pageTitle)
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

    private void WriteUrlsToFile(Dictionary<string, List<string>> statusCodes)
    {
        foreach (KeyValuePair<string, List<string>> kvp in statusCodes)
        {
            string statusCode = kvp.Key;
            List<string> urls = kvp.Value;

            File.WriteAllLines(statusCode + ".txt", urls);
        }
    }

    private int GetCountOfStatusCode(string statusCode, string targetStatusCode)
    {
        return statusCode == targetStatusCode && statusCodes.ContainsKey(statusCode) ? statusCodes[statusCode].Count : 0;
    }

    private IRestResponse GetResponse(string url)
    {
        RestRequest request = new RestRequest(url);
        IRestResponse response = client.Execute(request);
        return response;
    }

}
