using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace YoutubeCommunityScraper
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: YoutubeCommunityScraper <channel_url> [-o <output_file>] [-l <limit>]");
                return;
            }

            string channelUrl = args[0];
            string outputFile = null;
            int? limit = null;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length)
                {
                    outputFile = args[i + 1];
                    i++;
                }
                else if (args[i] == "-l" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int l))
                    {
                        limit = l;
                    }
                    i++;
                }
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                // Default output filename based on channel name or timestamp
                outputFile = "posts.json";
            }

            Console.WriteLine($"Scraping posts from {channelUrl}...");
            if (limit.HasValue) Console.WriteLine($"Limit: {limit}");

            var scraper = new Scraper();
            try
            {
                var posts = await scraper.GetPostsAsync(channelUrl, limit);
                
                Console.WriteLine($"Found {posts.Count} posts.");

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(posts, options);
                
                await File.WriteAllTextAsync(outputFile, jsonString);
                Console.WriteLine($"Saved to {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
