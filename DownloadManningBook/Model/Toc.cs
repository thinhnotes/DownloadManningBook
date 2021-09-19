using System.Collections.Generic;

namespace DownloadManningBook.Model
{
    public class Toc
    {
        public List<FrontMatter> FrontMatter { get; set; }
        public List<TocMeta> Parts { get; set; }
        public List<BackMatter> BackMatter { get; set; }
    }

    public class Data
    {
        public Toc Toc { get; set; }
    }
}
