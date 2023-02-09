using System.Collections.Generic;
using System;

namespace WebApplication1.Models
{
    public class ExplorerModel
    {
        public List<DirModel> Dirs;
        public List<FileModel> Files;

        public String Name;
        public String ParentName;
        public String URL;

        public ExplorerModel(List<DirModel> dirs, List<FileModel> files)
        {
            Dirs = dirs;
            Files = files;
        }
    }
}
