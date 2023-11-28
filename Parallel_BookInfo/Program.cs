using Parallel_BookInfo;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

internal class Program
{
    public enum DataRetrievalType
    {
        Server = 1,
        Cache = 2
    }

    private static async Task Main(string[] args)
    {
        var booksCache = new Dictionary<string, BookInfo>();
        var lines = ReadFile("assets/ISBN_Input_File.txt");
        var booksInfoCSV = new List<BookInfo>();

        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;

                var lineISBNs = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var notFoundISBNsInCache = new HashSet<string>();
                foreach (var ISBN in lineISBNs)
                    if (!booksCache.ContainsKey(ISBN))
                        notFoundISBNsInCache.Add(ISBN);

                foreach (var bookFromAPI in await ConsumeBookApiAsync(notFoundISBNsInCache))
                    booksCache.Add(bookFromAPI.ISBN, bookFromAPI);

                foreach (var ISBN in lineISBNs)
                {
                    if (booksCache.ContainsKey(ISBN))
                    {
                        var bookCache = booksCache[ISBN];
                        booksInfoCSV.Add(new BookInfo(i + 1, notFoundISBNsInCache.Contains(bookCache.ISBN) ? DataRetrievalType.Server : DataRetrievalType.Cache, bookCache.ISBN, bookCache.Title, bookCache.Subtitle, bookCache.AuthorNames, bookCache.NumberOfPages, bookCache.PublishDate));
                    }
                }
            }
        }

        WriteToCsv("output/ISBN_Output_File.csv", booksInfoCSV);

        Console.ReadKey();
    }

    private static void WriteToCsv(string filePath, List<BookInfo> bookInfos)
    {
        var csvContent = new StringBuilder();
        csvContent.AppendLine("Row Number;Data Retrieval Type;ISBN;Title;Subtitle;Author Name(s);Number of Pages;Publish Date");

        foreach (var bookInfo in bookInfos)
            csvContent.AppendLine(bookInfo.ToCSVFormat());

        try
        {
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var fullFilePath = $"{projectDirectory}/{filePath}";

            if (!Directory.Exists(projectDirectory + "\\output"))
                Directory.CreateDirectory(projectDirectory + "\\output");

            Console.WriteLine($"Writing {filePath}...\n");
            File.WriteAllText(fullFilePath, csvContent.ToString());

            Console.WriteLine("File successfully written!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while writing file {filePath}:\n{ex.Message}\n");
        }
    }

    private static string[]? ReadFile(string filePath)
    {
        try
        {
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var fullFilePath = $"{projectDirectory}/{filePath}";

            Console.WriteLine($"Reading {filePath}...\n");
            return File.ReadAllLines(fullFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading file {filePath}:\n{ex.Message}\n");
        }

        return null;
    }

    private static async Task<ICollection<BookInfo>> ConsumeBookApiAsync(ICollection<string> ISBNs)
    {
        var booksInfo = new List<BookInfo>();
        string apiUrl = $"https://openlibrary.org/api/books?bibkeys=ISBN:{string.Join(",ISBN:", ISBNs)}&jscmd=data&format=json";

        Console.WriteLine("Initiating API request...\n");

        using (var client = new HttpClient())
        {
            try
            {
                Console.WriteLine($"Sending GET request to: {apiUrl}\n");

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("API request successful!\n");

                    // Reading the response content as a string
                    string apiResponse = await response.Content.ReadAsStringAsync();

                    // Processing the response content (you may customize this part)
                    Console.WriteLine("Processing API response...\n");

                    // Deserializing the JSON response to a dynamic object
                    dynamic dynamicJson = JsonNode.Parse(apiResponse);
                    // Itarate through jsons and cast to BookInfo
                    foreach (var elem in dynamicJson)
                    {
                        var val = elem.Value;

                        string subtitle = null;
                        if (val["subtitle"] != null)
                            subtitle = val["subtitle"].ToString();

                        var authorsName = new StringBuilder();
                        foreach (var authorName in val["authors"])
                            authorsName.Append($"{(authorsName.Length > 0 ? ";" : "")}{authorName["name"]}");

                        string number_of_pages = null;
                        if (val["number_of_pages"] != null)
                            number_of_pages = val["number_of_pages"].ToString();

                        string publish_date = null;
                        if (val["publish_date"] != null)
                            publish_date = val["publish_date"].ToString();

                        booksInfo.Add(new BookInfo(0, DataRetrievalType.Server, elem.Key.Replace("ISBN:", ""), val["title"].ToString(),
                            subtitle, authorsName.ToString(), number_of_pages, publish_date));
                    }

                    // Displaying the API response content
                    //Console.WriteLine($"API Response:\n{apiResponse}\n");
                }
                else
                {
                    Console.WriteLine($"Error in the API request. Status code: {response.StatusCode}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during API request: {ex.Message}\n");
            }
        }

        Console.WriteLine("API request and response process completed.\n");

        return booksInfo;
    }
}