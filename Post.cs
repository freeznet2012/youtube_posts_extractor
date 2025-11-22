using System;
using System.Collections.Generic;

namespace YoutubeCommunityScraper
{
    public class Post
    {
        public string PostId { get; set; }
        public string PostLink { get; set; }
        public string Text { get; set; }
        public string TimeSince { get; set; }
        public string TimeOfDownload { get; set; }
        public string VideoLink { get; set; }
        public List<string> ImageLinks { get; set; } = new List<string>();
        public bool HasPoll { get; set; }
    }
}
