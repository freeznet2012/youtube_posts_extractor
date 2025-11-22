using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace YoutubeCommunityScraper
{
    public class Scraper
    {
        private readonly HttpClient _httpClient;
        private const string DefaultSocsCookie = "CAESEwgDEgk2NDg4NTY2OTgaAnJvIAEaBgiAtae0Bg";
        private const string CookiePath = "cookies.txt";

        public Scraper()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        }

        private string GetSocsCookie()
        {
            if (File.Exists(CookiePath))
            {
                return File.ReadAllText(CookiePath).Trim();
            }
            return DefaultSocsCookie;
        }

        public async Task<List<Post>> GetPostsAsync(string channelUrl, int? limit = null)
        {
            var posts = new List<Post>();
            
            // Ensure URL ends with /posts
            string cleanUrl = channelUrl.TrimEnd('/');
            if (!cleanUrl.EndsWith("/posts"))
            {
                cleanUrl += "/posts";
            }

            // Set Cookie
            if (!_httpClient.DefaultRequestHeaders.Contains("Cookie"))
            {
                 _httpClient.DefaultRequestHeaders.Add("Cookie", $"SOCS={GetSocsCookie()}");
            }

            // 1. Initial Request
            var response = await _httpClient.GetStringAsync(cleanUrl + "?persist_hl=1&hl=en");
            
            // 2. Extract API Key, API URL, and Initial Token
            var apiKey = ExtractRegex(response, "(?<=\"INNERTUBE_API_KEY\":\")(.+?)(?=\")");
            var apiUrl = ExtractRegex(response, "(?<=\"apiUrl\":\")(.+?)(?=\")");
            var initialToken = ExtractRegex(response, "(?<=\"token\":\")(.+?)(?=\")");

            // Extract Initial Posts
            var initialPosts = ExtractInitialPosts(response);
            posts.AddRange(initialPosts);

            if (posts.Count == 0)
            {
                Console.WriteLine("No initial posts found. Saving HTML to debug.html...");
                await File.WriteAllTextAsync("debug.html", response);

                // Extract and save ytInitialData for inspection
                var dataMatch = Regex.Match(response, @"var ytInitialData = (\{.+?\});</script>", RegexOptions.Singleline);
                if (dataMatch.Success)
                {
                    Console.WriteLine("Saving ytInitialData to debug_data.json...");
                    await File.WriteAllTextAsync("debug_data.json", dataMatch.Groups[1].Value);
                }
                else
                {
                    Console.WriteLine("Could not extract ytInitialData from HTML.");
                }
            }

            if (limit.HasValue && posts.Count >= limit.Value)
            {
                return posts.Take(limit.Value).ToList();
            }

            // 3. Continuation Loop
            string continuationToken = initialToken;
            string channelName = channelUrl.Split('/').Last();

            while (!string.IsNullOrEmpty(continuationToken) && (!limit.HasValue || posts.Count < limit.Value))
            {
                try 
                {
                    var (newPosts, newToken) = await FetchContinuationAsync(apiKey, apiUrl, continuationToken, cleanUrl);
                    
                    posts.AddRange(newPosts);
                    continuationToken = newToken;

                    // Stop if no new token and no new posts (end of feed)
                    if (string.IsNullOrEmpty(newToken) && newPosts.Count == 0)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching continuation: {ex.Message}");
                    break;
                }
            }

            // Python code reverses the posts at the end (oldest first), but CLI usually expects newest first.
            // The Python code has a --reverse flag. Default seems to be oldest first?
            // "self.posts.reverse() # reverses post order from newest first to oldest first"
            // Wait, if it appends, it collects newest first (top of page). 
            // Then it reverses to be oldest first.
            // I will keep them as is (newest first) unless requested otherwise, as that's standard for "GetPosts".
            
            if (limit.HasValue)
            {
                return posts.Take(limit.Value).ToList();
            }

            return posts;
        }

        private string ExtractRegex(string content, string pattern)
        {
            var match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private List<Post> ExtractInitialPosts(string html)
        {
            var posts = new List<Post>();
            // Python Regex: ({\"backstagePostThreadRenderer\":)(.+?)(\"}}}})(?=(,{)|(],))
            // We need to be careful with the lookahead in C#.
            
            var regex = new Regex("(\\{\"backstagePostThreadRenderer\":)(.+?)(\"}}}})(?=(,\\{)|(],))");
            var matches = regex.Matches(html);

            foreach (Match match in matches)
            {
                try
                {
                    // Reconstruct valid JSON
                    // Python: json.loads(post[1] + post[2][:-1]) 
                    // post[1] is group 2 (body), post[2] is group 3 ("}}}}")
                    // Python slice [:-1] removes the last char of "}}}}" -> "}}}"
                    // So it constructs: {"backstagePostThreadRenderer": BODY "}}}"
                    // Wait, group 1 is {"backstagePostThreadRenderer":
                    
                    // Let's look at Python again:
                    // posts = re.findall(pattern="({\"backstagePostThreadRenderer\":)(.+?)(\"}}}}(?=(,{)|(],)))", string=string)
                    // json_posts = [json.loads(post[1] + post[2][:-1]) for post in posts]
                    // post[0] is full match? No, findall returns tuples of groups.
                    // Group 1: {"backstagePostThreadRenderer":
                    // Group 2: BODY
                    // Group 3: "}}}}
                    
                    // So it combines: BODY + "}}}"
                    // Wait, where does the opening brace come from?
                    // Ah, post[1] is the BODY.
                    // post[2] is "}}}}".
                    // post[2][:-1] is "}}}".
                    // So it parses: BODY + "}}}"
                    // This seems to imply BODY starts with { ?
                    // No, the regex is ({\"backstagePostThreadRenderer\":)(.+?)
                    // So Group 1 is `{"backstagePostThreadRenderer":`
                    // Group 2 is the content value?
                    
                    // Let's try to just parse the full match as a JSON object property?
                    // Or construct a wrapper.
                    
                    string jsonStr = "{" + match.Groups[2].Value + "}}}"; 
                    // We are adding { at start.
                    // Group 2 is the value of the property?
                    // If the text is `{"backstagePostThreadRenderer":{"post":...}}}}`
                    // Group 1: `{"backstagePostThreadRenderer":`
                    // Group 2: `{"post":...`
                    // Group 3: `}}}}`
                    
                    // If we take Group 2 + "}}}", we get `{"post":...}}}`
                    // That looks like a valid object `{"post":...}`
                    
                    using var doc = JsonDocument.Parse(jsonStr);
                    var root = doc.RootElement;
                    
                    // Now root is the object inside backstagePostThreadRenderer?
                    // Let's check if it has "post"
                    
                    if (root.TryGetProperty("post", out var postObj))
                    {
                        if (postObj.TryGetProperty("backstagePostRenderer", out var backstagePost))
                        {
                            posts.Add(ParsePost(backstagePost));
                        }
                        else if (postObj.TryGetProperty("sharedPostRenderer", out var sharedPost))
                        {
                            posts.Add(ParsePost(sharedPost));
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }

            return posts;
        }

        private async Task<(List<Post>, string)> FetchContinuationAsync(string apiKey, string apiUrl, string token, string originalUrl)
        {
            var url = $"https://www.youtube.com{apiUrl}?key={apiKey}&prettyPrint=false";
            
            var payload = new
            {
                context = new
                {
                    client = new
                    {
                        userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                        clientName = "WEB",
                        clientVersion = "2.20231010.10.01",
                        originalUrl = originalUrl,
                        platform = "DESKTOP",
                        browserName = "Chrome",
                        browserVersion = "139.0.0.0",
                        acceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                        utcOffsetMinutes = 0
                    }
                },
                continuation = token
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();



            var posts = new List<Post>();
            string nextToken = null;

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("onResponseReceivedEndpoints", out var endpoints))
            {
                foreach (var endpoint in endpoints.EnumerateArray())
                {
                    if (endpoint.TryGetProperty("appendContinuationItemsAction", out var action))
                    {
                        if (action.TryGetProperty("continuationItems", out var items))
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                if (item.TryGetProperty("backstagePostThreadRenderer", out var threadRenderer))
                                {
                                    if (threadRenderer.TryGetProperty("post", out var postObj))
                                    {
                                        if (postObj.TryGetProperty("backstagePostRenderer", out var backstagePost))
                                        {
                                            posts.Add(ParsePost(backstagePost));
                                        }
                                        else if (postObj.TryGetProperty("sharedPostRenderer", out var sharedPost))
                                        {
                                            posts.Add(ParsePost(sharedPost));
                                        }
                                    }
                                }
                                else if (item.TryGetProperty("continuationItemRenderer", out var continuationItem))
                                {
                                    if (continuationItem.TryGetProperty("continuationEndpoint", out var contEndpoint))
                                    {
                                        if (contEndpoint.TryGetProperty("continuationCommand", out var command))
                                        {
                                            if (command.TryGetProperty("token", out var t))
                                            {
                                                nextToken = t.GetString();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (posts, nextToken);
        }

        private Post ParsePost(JsonElement postJson)
        {
            var post = new Post();
            
            if (postJson.TryGetProperty("postId", out var id))
                post.PostId = id.GetString();
            
            post.PostLink = $"https://www.youtube.com/post/{post.PostId}";
            
            if (postJson.TryGetProperty("publishedTimeText", out var timeText))
            {
                if (timeText.TryGetProperty("runs", out var runs) && runs.GetArrayLength() > 0)
                {
                    post.TimeSince = runs[0].GetProperty("text").GetString();
                }
            }

            post.TimeOfDownload = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // Text
            if (postJson.TryGetProperty("contentText", out var contentText))
            {
                post.Text = ExtractText(contentText);
            }
            else if (postJson.TryGetProperty("content", out var content)) // sharedPostRenderer
            {
                post.Text = ExtractText(content);
            }

            // Images
            if (postJson.TryGetProperty("backstageAttachment", out var attachment))
            {
                if (attachment.TryGetProperty("backstageImageRenderer", out var imageRenderer))
                {
                    var url = GetThumbnailUrl(imageRenderer);
                    if (url != null) post.ImageLinks.Add(url);
                }
                else if (attachment.TryGetProperty("postMultiImageRenderer", out var multiImage))
                {
                    if (multiImage.TryGetProperty("images", out var images))
                    {
                        foreach (var img in images.EnumerateArray())
                        {
                            if (img.TryGetProperty("backstageImageRenderer", out var ir))
                            {
                                var url = GetThumbnailUrl(ir);
                                if (url != null) post.ImageLinks.Add(url);
                            }
                        }
                    }
                }
                else if (attachment.TryGetProperty("videoRenderer", out var videoRenderer))
                {
                    if (videoRenderer.TryGetProperty("videoId", out var vid))
                    {
                        post.VideoLink = $"https://www.youtube.com/watch?v={vid.GetString()}";
                    }
                }
            }

            return post;
        }

        private string ExtractText(JsonElement contentText)
        {
            if (contentText.TryGetProperty("runs", out var runs))
            {
                var text = "";
                foreach (var run in runs.EnumerateArray())
                {
                    // Handle navigation endpoints (links)
                    if (run.TryGetProperty("navigationEndpoint", out var nav))
                    {
                         if (nav.TryGetProperty("urlEndpoint", out var urlEndpoint))
                         {
                             if (urlEndpoint.TryGetProperty("url", out var url))
                             {
                                 // Simple extraction, Python does more complex regex unquoting
                                 text += url.GetString(); 
                                 continue;
                             }
                         }
                    }

                    if (run.TryGetProperty("text", out var t))
                    {
                        text += t.GetString();
                    }
                }
                return text;
            }
            return "";
        }

        private string GetThumbnailUrl(JsonElement imageRenderer)
        {
            if (imageRenderer.TryGetProperty("image", out var image))
            {
                if (image.TryGetProperty("thumbnails", out var thumbnails))
                {
                    var thumbs = thumbnails.EnumerateArray().ToList();
                    if (thumbs.Count > 0)
                    {
                        // Get the last one (usually highest quality)
                        return thumbs[thumbs.Count - 1].GetProperty("url").GetString();
                    }
                }
            }
            return null;
        }
    }
}
