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
        // Dictionary to store cached BookInfo objects using ISBN as the key
        var booksCache = new Dictionary<string, BookInfo>();

        // Reading ISBNs from a file
        var lines = ReadFile("assets/ISBN_Input_File.txt");

        // List to store BookInfo objects for each ISBN
        var booksInfoCSV = new List<BookInfo>();

        // Check if the file was read successfully
        if (lines != null)
        {
            // Loop through each line in the file
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Skip empty lines
                if (string.IsNullOrEmpty(line)) continue;

                // Split the line into ISBNs
                var lineISBNs = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // HashSet to store ISBNs not found in the cache
                var notFoundISBNsInCache = new HashSet<string>();

                // Check if ISBN is not in the cache and add it to the set
                foreach (var ISBN in lineISBNs)
                    if (!booksCache.ContainsKey(ISBN))
                        notFoundISBNsInCache.Add(ISBN);

                // Retrieve BookInfo from API for ISBNs not found in the cache
                if (notFoundISBNsInCache.Count > 0)
                    foreach (var bookFromAPI in await ConsumeBookApiAsync(notFoundISBNsInCache))
                        booksCache.Add(bookFromAPI.ISBN, bookFromAPI);

                // Process each ISBN in the line
                foreach (var ISBN in lineISBNs)
                {
                    // Check if ISBN is in the cache
                    if (booksCache.ContainsKey(ISBN))
                    {
                        // Retrieve BookInfo from cache
                        var bookCache = booksCache[ISBN];

                        // Add BookInfo to the list with details and retrieval type
                        booksInfoCSV.Add(new BookInfo(i + 1, notFoundISBNsInCache.Contains(bookCache.ISBN) ? DataRetrievalType.Server : DataRetrievalType.Cache, bookCache.ISBN, bookCache.Title, bookCache.Subtitle, bookCache.AuthorNames, bookCache.NumberOfPages, bookCache.PublishDate));
                    }
                }
            }
        }

        // Write the list of BookInfo objects to a CSV file
        WriteToCsv("output/ISBN_Output_File.csv", booksInfoCSV);

        // Wait for user input before closing the console
        Console.ReadKey();
    }

    private static void WriteToCsv(string filePath, ICollection<BookInfo> bookInfos)
    {
        // StringBuilder to construct CSV content
        var csvContent = new StringBuilder();

        // Adding header to CSV content
        csvContent.AppendLine("Row Number;Data Retrieval Type;ISBN;Title;Subtitle;Author Name(s);Number of Pages;Publish Date");

        // Adding each BookInfo object to CSV content
        foreach (var bookInfo in bookInfos)
            csvContent.AppendLine(bookInfo.ToCSVFormat());

        try
        {
            // Get the full file path
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var fullFilePath = $"{projectDirectory}/{filePath}";

            // Create the output directory if it doesn't exist
            if (!Directory.Exists(projectDirectory + "\\output"))
                Directory.CreateDirectory(projectDirectory + "\\output");

            // Write CSV content to the file
            Console.WriteLine($"Writing {filePath}...\n");
            File.WriteAllText(fullFilePath, csvContent.ToString());

            // Display success message
            Console.WriteLine("File successfully written!\n");
        }
        catch (Exception ex)
        {
            // Display error message if an exception occurs
            Console.WriteLine($"An error occurred while writing file {filePath}:\n{ex.Message}\n");
        }
    }

    private static string[]? ReadFile(string filePath)
    {
        try
        {
            // Get the full file path
            var projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var fullFilePath = $"{projectDirectory}/{filePath}";

            // Display reading message
            Console.WriteLine($"Reading {filePath}...\n");

            // Read all lines from the file
            return File.ReadAllLines(fullFilePath);
        }
        catch (Exception ex)
        {
            // Display error message if an exception occurs during file reading
            Console.WriteLine($"An error occurred while reading file {filePath}:\n{ex.Message}\n");
        }

        // Return null if an exception occurs
        return null;
    }

    // Asynchronous method to consume Book API for a collection of ISBNs
    private static async Task<ICollection<BookInfo>> ConsumeBookApiAsync(ICollection<string> ISBNs)
    {
        // List to store BookInfo objects retrieved from the API
        var booksInfo = new List<BookInfo>();

        if (ISBNs.Count == 0)
            return booksInfo;

        // API URL for fetching book information using ISBNs
        string apiUrl = $"https://openlibrary.org/api/books?bibkeys=ISBN:{string.Join(",ISBN:", ISBNs)}&jscmd=data&format=json";

        // Display API request initiation message
        Console.WriteLine("Initiating API request...\n");

        // Using HttpClient to make asynchronous API request
        using (var client = new HttpClient())
        {
            try
            {
                // Display the API request URL
                Console.WriteLine($"Sending GET request to: {apiUrl}\n");

                // Send the GET request to the API
                var response = await client.GetAsync(apiUrl);

                // Check if the API request is successful
                if (response.IsSuccessStatusCode)
                {
                    // Display success message
                    Console.WriteLine("API request successful!\n");

                    // Reading the response content as a string
                    string apiResponse = await response.Content.ReadAsStringAsync();

                    // Processing the API response
                    Console.WriteLine("Processing API response...\n");

                    // Deserializing the JSON response to a dynamic object
                    dynamic dynamicJson = JsonNode.Parse(apiResponse);

                    // Iterate through JSON elements and cast to BookInfo
                    foreach (var elem in dynamicJson)
                    {
                        var val = elem.Value;

                        // Extracting optional fields from the JSON response
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

                        // Adding BookInfo to the list
                        booksInfo.Add(new BookInfo(0, DataRetrievalType.Server, elem.Key.Replace("ISBN:", ""), val["title"].ToString(),
                            subtitle, authorsName.ToString(), number_of_pages, publish_date));
                    }

                    // Displaying the API response content (optional)
                    // Console.WriteLine($"API Response:\n{apiResponse}\n");
                }
                else
                {
                    // Display error message if the API request is not successful
                    Console.WriteLine($"Error in the API request. Status code: {response.StatusCode}\n");
                }
            }
            catch (Exception ex)
            {
                // Display error message if an exception occurs during API request
                Console.WriteLine($"Error during API request: {ex.Message}\n");
            }
        }

        // Display completion message for API request and response processing
        Console.WriteLine("API request and response process completed.\n");

        // Return the list of BookInfo objects retrieved from the API
        return booksInfo;
    }
}