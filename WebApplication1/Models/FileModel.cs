using System;

namespace WebApplication1.Models
{
    public class FileModel
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public DateTime Accessed { get; set; }
    }
}
