using System.Collections.Generic;

namespace DownloadManningBook.Model
{
    public class Toc
    {
        public List<TocMeta> Parts { get; set; }
    }

    public class Data
    {
        public Toc Toc { get; set; }
    }
}
