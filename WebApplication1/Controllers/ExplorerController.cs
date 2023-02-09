using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using WebApplication1.Models;
using static System.Net.WebRequestMethods;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Caching.Memory;

namespace WebApplication1.Controllers
{
    public class ExplorerController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private IMemoryCache _cache;
        public ExplorerController(IMemoryCache memoryCache,
                               IHttpContextAccessor httpContextAccessor)
        {
            _cache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index(string path)
        {
            string rootPath = @"C:\";
            DirectoryInfo folderPath = new DirectoryInfo(rootPath);
            var realPath = folderPath + path;
            if (Directory.Exists(realPath))
            {
                List<DirModel> dirs = MapDirs(realPath);
                List<FileModel> files = MapFiles(realPath);
                ExplorerModel explorerModel = new ExplorerModel(dirs, files);
                if (realPath.Last() != '/' && realPath.Last() != '\\')
                    explorerModel.URL = "/Explorer/" + path + "/";
                else
                    explorerModel.URL = "/Explorer/" + path;
                var request = _httpContextAccessor.HttpContext.Request;
                UriBuilder uriBuilder = new UriBuilder
                {
                    Path = request.Path.ToString()
                };
                explorerModel.Name = WebUtility.UrlDecode(uriBuilder.Uri.Segments.Last());
                Uri uri = new Uri(uriBuilder.Uri.AbsoluteUri.Remove
                            (uriBuilder.Uri.AbsoluteUri.Length -
                             uriBuilder.Uri.Segments.Last().Length));
                explorerModel.ParentName = uri.AbsolutePath;
                return View(explorerModel);
            }
            else
            {
                return Content(path + " is not a valid file or directory.");
            }

        }

        private List<FileModel> MapFiles(string realPath)
        {
            List<FileModel> filesModel = new List<FileModel>();
            IEnumerable<string> files = null;
            try
            {
                files = Directory.EnumerateFiles(realPath);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            if (files != null)
            {
                foreach (string file in files)
                {
                    FileInfo f = new FileInfo(file);
                    FileModel fileModel = new FileModel();
                    {
                        fileModel.Name = Path.GetFileName(file);
                        fileModel.Accessed = f.LastAccessTime;
                        fileModel.Size = GetOnDiskSize(f.FullName) / 1024;
                        filesModel.Add(fileModel);
                    }
                }
            }
            return filesModel;
        }

        private List<DirModel> MapDirs(string realPath)
        {
            List<DirModel> dirsModel = new List<DirModel>();
            IEnumerable<string> dirs = null;
            try
            {
                dirs = Directory.EnumerateDirectories(realPath);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            if (dirs != null)
            {
                foreach (string dir in dirs)
                {
                    DirectoryInfo d = new DirectoryInfo(dir);
                    DirModel dirModel = new DirModel();
                    {
                        dirModel.Id = dir.GetHashCode().ToString();
                        dirModel.Name = Path.GetFileName(dir);
                        dirModel.Accessed = d.LastAccessTime;
                        dirModel.Path = d.FullName;
                        dirModel.IsNotEmpty = IsNotEmpty(d);
                    }

                    dirsModel.Add(dirModel);
                }
            }
            return dirsModel;
        }

        public static long GetDirectorySize(DirectoryInfo directoryInfo, bool recursive = true)
        {
            var startDirectorySize = default(long);
            if (directoryInfo == null || !directoryInfo.Exists)
                return startDirectorySize;
            
            FileInfo[] files = null;
            try
            {
                files = directoryInfo.GetFiles();
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            if (files != null)
            {
                foreach (var fileInfo in files)
                    Interlocked.Add(ref startDirectorySize, fileInfo.Length);
                if (recursive)
                    Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) =>
                Interlocked.Add(ref startDirectorySize, GetDirectorySize(subDirectory, recursive)));
            }
            return startDirectorySize;
        }

        public static bool IsNotEmpty(DirectoryInfo d)
        {
            bool result = false;
            try
            {
                result = d.GetDirectories().Any();
            }
            catch (UnauthorizedAccessException) { result = false; }
            catch (DirectoryNotFoundException) { }
            return result;
        }

        [HttpPost]
        public string CalcDirSize(string path)
        {
            long dirSizeOnDisk;
            if (!_cache.TryGetValue(path, out dirSizeOnDisk))
            {
                dirSizeOnDisk = GetFolderSize(path) / 1024;
                _cache.Set(path, dirSizeOnDisk,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }
            return dirSizeOnDisk.ToString();
        }
        private static long GetFolderSize(string sourceDir)
        {
            long size = 0;
            IEnumerable<string> fileEntries = null;
            try
            {
                fileEntries = Directory.EnumerateFiles(sourceDir);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            if (fileEntries != null)
            {
                foreach (string fileName in fileEntries)
                {
                    Interlocked.Add(ref size, (GetOnDiskSize(new FileInfo(fileName).ToString())));
                }
            }
            IEnumerable<string> subFolders = null;
            try
            {
                subFolders = Directory.EnumerateDirectories(sourceDir);
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            if (subFolders != null)
            {
                var tasks = subFolders.Select(folder => Task.Factory.StartNew(() =>
                {
                    if ((System.IO.File.GetAttributes(folder) & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        Interlocked.Add(ref size, (GetFolderSize(folder)));
                        return size;
                    }
                    return 0;
                }));

                Task.WaitAll(tasks.ToArray());
            }
            return size;
        }

        internal static Int64 GetOnDiskSize(string path)
        {
            try
            {
                using (var fStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fileInfo = new FILE_STANDARD_INFO();
                    GetFileInformationByHandleEx(fStream.Handle, FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, out fileInfo, (uint)Marshal.SizeOf(fileInfo));
                    var win32Error = Marshal.GetLastWin32Error();
                    if (win32Error != 0)
                        throw new Win32Exception();
                    Int64 result = fileInfo.AllocationSize.QuadPart;
                    if (result < 4096)
                        return 0;
                    return result;
                }
            }

            catch (UnauthorizedAccessException) { return 0; }
            catch (System.IO.IOException) { return 0; }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandleEx(
            IntPtr hFile,
            FILE_INFO_BY_HANDLE_CLASS infoClass,
            out FILE_STANDARD_INFO fileInfo,
            uint dwBufferSize);

        private enum FILE_INFO_BY_HANDLE_CLASS
        {
            FileBasicInfo = 0,
            FileStandardInfo = 1,
            FileNameInfo = 2,
            FileRenameInfo = 3,
            FileDispositionInfo = 4,
            FileAllocationInfo = 5,
            FileEndOfFileInfo = 6,
            FileStreamInfo = 7,
            FileCompressionInfo = 8,
            FileAttributeTagInfo = 9,
            FileIdBothDirectoryInfo = 10,// 0x0A
            FileIdBothDirectoryRestartInfo = 11, // 0xB
            FileIoPriorityHintInfo = 12, // 0xC
            FileRemoteProtocolInfo = 13, // 0xD
            FileFullDirectoryInfo = 14, // 0xE
            FileFullDirectoryRestartInfo = 15, // 0xF
            FileStorageInfo = 16, // 0x10
            FileAlignmentInfo = 17, // 0x11
            FileIdInfo = 18, // 0x12
            FileIdExtdDirectoryInfo = 19, // 0x13
            FileIdExtdDirectoryRestartInfo = 20, // 0x14
            MaximumFileInfoByHandlesClass
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FILE_STANDARD_INFO
        {
            public LargeInteger AllocationSize;
            public LargeInteger EndOfFile;
            public uint NumberOfLinks;
            public byte DeletePending;
            public byte Directory;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct LargeInteger
        {
            [FieldOffset(0)]
            public int Low;
            [FieldOffset(4)]
            public int High;
            [FieldOffset(0)]
            public long QuadPart;

            public long ToInt64()
            {
                return ((long)this.High << 32) | (uint)this.Low;
            }
            public static LargeInteger FromInt64(long value)
            {
                return new LargeInteger
                {
                    Low = (int)(value),
                    High = (int)((value >> 32))
                };
            }

        }
    }
}
