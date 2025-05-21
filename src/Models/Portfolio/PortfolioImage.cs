using System;

namespace BlogReaderFunction.Models.Portfolio
{
    public class PortfolioImage
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string Url { get; set; }
        public DateTime UploadDate { get; set; }
        public string PortfolioPostId { get; set; }
        public string AltText { get; set; }
        public string Caption { get; set; }
        public bool IsThumbnail { get; set; }
    }
}