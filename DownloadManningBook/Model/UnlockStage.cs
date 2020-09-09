using System.Collections.Generic;

namespace DownloadManningBook.Model
{
    public class UnlockStage
    {
        public bool Success { get; set; }
        public double PreviewDuration { get; set; }
        public string Reason { get; set; }
        public List<UnlockedParagraphs> UnlockedParagraphs { get; set; }
    }

    public class UnlockedParagraphs
    {
        public string ParagraphId { get; set; }
        public string Content { get; set; }
        public string HashId { get; set; }
    }
}
