using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManningBook.Model
{
    public class Chapter
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string ShortName { get; set; }
        public bool IsFree { get; set; }
        public List<Section> Sections { get; set; }
    }

}
