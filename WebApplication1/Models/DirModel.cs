using System;

namespace WebApplication1.Models
{
    public class DirModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime Accessed { get; set; }
        public bool IsNotEmpty { get; set; }
    }
}
