# YouTube Community Posts Scraper

A .NET console application that scrapes community posts from YouTube channels and exports them to JSON format.

## Features

- 📝 Extract text content from community posts
- 🖼️ Download image URLs from posts
- 🎥 Capture video links shared in posts
- 🔢 Support for limiting the number of posts to scrape
- 💾 Export data to JSON format with customizable output filename
- 🔄 Automatic pagination through all available posts
- 🍪 Cookie support for bypassing consent screens

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher

## Installation

1. Clone the repository:
```bash
git clone https://github.com/freeznet2012/youtube_posts_extractor.git
cd youtube_posts_extractor
```

2. Build the project:
```bash
dotnet build
```

## Usage

### Basic Usage

```bash
dotnet run -- <channel_url>
```

### Command-Line Options

| Option | Description | Example |
|--------|-------------|---------|
| `<channel_url>` | YouTube channel URL (required) | `https://www.youtube.com/channel/UCX6OQ3DkcsbYNE6H8uQQuVA` |
| `-o <output_file>` | Output JSON filename (default: `posts.json`) | `-o output.json` |
| `-l <limit>` | Limit the number of posts to fetch | `-l 10` |

### Examples

**Scrape all community posts from a channel:**
```bash
dotnet run -- "https://www.youtube.com/channel/UCX6OQ3DkcsbYNE6H8uQQuVA"
```

**Scrape only the latest 5 posts:**
```bash
dotnet run -- "https://www.youtube.com/channel/UCX6OQ3DkcsbYNE6H8uQQuVA" -l 5
```

**Save to a custom output file:**
```bash
dotnet run -- "https://www.youtube.com/channel/UCX6OQ3DkcsbYNE6H8uQQuVA" -o mrbeast_posts.json -l 10
```

### Supported URL Formats

The scraper supports various YouTube channel URL formats:
- `https://www.youtube.com/channel/CHANNEL_ID`
- `https://www.youtube.com/@USERNAME`
- `https://www.youtube.com/c/CHANNEL_NAME`

The scraper automatically appends `/posts` to the URL if not present.

## Output Format

The scraper exports data as a JSON array containing post objects with the following structure:

```json
[
  {
    "PostId": "UgkxuHoYhDNrbrhuYVFh_Nzuo2vZ239YDpdu",
    "PostLink": "https://www.youtube.com/post/UgkxuHoYhDNrbrhuYVFh_Nzuo2vZ239YDpdu",
    "Text": "Post content text...",
    "TimeSince": "3 months ago",
    "TimeOfDownload": "2025-11-22 20:54:00",
    "VideoLink": "https://www.youtube.com/watch?v=VIDEO_ID",
    "ImageLinks": [
      "https://yt3.ggpht.com/IMAGE_URL_1",
      "https://yt3.ggpht.com/IMAGE_URL_2"
    ],
    "HasPoll": false
  }
]
```

### Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `PostId` | string | Unique identifier for the post |
| `PostLink` | string | Direct URL to the community post |
| `Text` | string | Text content of the post |
| `TimeSince` | string | Relative time since post was created (e.g., "3 months ago") |
| `TimeOfDownload` | string | Timestamp when the post was scraped |
| `VideoLink` | string | URL of embedded video (null if no video) |
| `ImageLinks` | array | List of image URLs attached to the post |
| `HasPoll` | boolean | Indicates if the post contains a poll |

## Advanced Configuration

### Custom Cookies

If you encounter issues with YouTube's consent screen or regional restrictions, you can provide a custom SOCS cookie:

1. Create a file named `cookies.txt` in the project root directory
2. Paste your SOCS cookie value into the file
3. Run the scraper normally

To obtain your SOCS cookie:
1. Visit YouTube in your browser
2. Open Developer Tools (F12)
3. Go to Application/Storage → Cookies → youtube.com
4. Find the `SOCS` cookie and copy its value

## Troubleshooting

### No posts found

If the scraper returns 0 posts:
- Verify the channel has community posts enabled
- Check that the URL is correct and includes the channel ID
- Try using the channel ID format instead of handle format
- Check the generated `debug.html` and `debug_data.json` files for details

### Rate limiting

If you're scraping many posts:
- Add delays between requests (requires code modification)
- Use the `-l` flag to limit the number of posts
- Run during off-peak hours

## Project Structure

```
youtube_posts_extractor/
├── Program.cs              # Main entry point and CLI argument parsing
├── Scraper.cs             # Core scraping logic and HTTP requests
├── Post.cs                # Post data model
├── YoutubeCommunityScraper.csproj  # Project configuration
└── README.md              # This file
```

## How It Works

1. **Initial Request**: The scraper fetches the channel's community posts page
2. **Extract API Keys**: Parses the HTML to extract YouTube's internal API key and continuation tokens
3. **Parse Posts**: Extracts post data using regex patterns matching YouTube's internal JSON structure
4. **Pagination**: Uses continuation tokens to fetch additional posts via YouTube's internal API
5. **Export**: Serializes all collected posts to JSON format

## Building from Source

### Debug Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build -c Release
```

### Create Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in `bin/Release/net9.0/win-x64/publish/`

## Dependencies

- .NET 9.0 Runtime
- System.Text.Json (included in .NET)
- System.Net.Http (included in .NET)

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## License

This project is provided as-is for educational and research purposes.

## Disclaimer

This tool is for educational purposes only. Be respectful of YouTube's Terms of Service and rate limits. Use responsibly and avoid excessive scraping that could impact YouTube's infrastructure.

## Author

freeznet2012

## Acknowledgments

Inspired by community needs for analyzing YouTube community post data for research and archival purposes.
