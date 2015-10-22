using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.S3
{
    public class S3VirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public S3VirtualPathProvider(IAppHost appHost)
            : base(appHost)
        {
            this.rootDirectory = new S3VirtualDirectory(this);
        }

        public S3VirtualDirectory rootDirectory;

        public override IVirtualDirectory RootDirectory
        {
            get { return rootDirectory; }
        }

        public override string VirtualPathSeparator
        {
            get { return "/"; }
        }

        public override string RealPathSeparator
        {
            get { return "/"; }
        }

        protected override void Initialize()
        {
        }

        public void AddFile(string filePath, string contents)
        {
            rootDirectory.AddFile(filePath, contents);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return rootDirectory.GetFile(virtualPath)
                ?? base.GetFile(virtualPath);
        }
    }

    public class S3VirtualDirectory : AbstractVirtualDirectoryBase
    {
        public S3VirtualDirectory(IVirtualPathProvider owningProvider)
            : base(owningProvider)
        {
            this.files = new List<S3VirtualFile>();
            this.dirs = new List<S3VirtualDirectory>();
            this.DirLastModified = DateTime.MinValue;
        }

        public S3VirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
            : base(owningProvider, parentDirectory)
        { }

        public DateTime DirLastModified { get; set; }
        public override DateTime LastModified
        {
            get { return DirLastModified; }
        }

        public List<S3VirtualFile> files;

        public override IEnumerable<IVirtualFile> Files
        {
            get { return files; }
        }

        public List<S3VirtualDirectory> dirs;

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return dirs; }
        }

        public string DirName { get; set; }
        public override string Name
        {
            get { return DirName; }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            virtualPath = StripBeginningDirectorySeparator(virtualPath);
            return files.FirstOrDefault(x => x.FilePath == virtualPath);
        }

        public override IEnumerator<IVirtualNode> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected override IVirtualFile GetFileFromBackingDirectoryOrDefault(string fileName)
        {
            return GetFile(fileName);
        }

        protected override IEnumerable<IVirtualFile> GetMatchingFilesInDir(string globPattern)
        {
            var matchingFilesInBackingDir = EnumerateFiles(globPattern).Cast<IVirtualFile>();
            return matchingFilesInBackingDir;
        }

        public IEnumerable<S3VirtualFile> EnumerateFiles(string pattern)
        {
            foreach (var file in files.Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
            foreach (var file in dirs.SelectMany(d => d.EnumerateFiles(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return null;
        }

        static readonly char[] DirSeps = new[] { '\\', '/' };
        public void AddFile(string filePath, string contents)
        {
            filePath = StripBeginningDirectorySeparator(filePath);
            this.files.Add(new S3VirtualFile(VirtualPathProvider, this)
            {
                FilePath = filePath,
                FileName = filePath.Split(DirSeps).Last(),
                TextContents = contents,
            });
        }

        private static string StripBeginningDirectorySeparator(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;

            if (DirSeps.Any(d => filePath[0] == d))
                return filePath.Substring(1);

            return filePath;
        }
    }

    public class S3VirtualFile : AbstractVirtualFileBase
    {
        public S3VirtualFile(IVirtualPathProvider owningProvider, IVirtualDirectory directory)
            : base(owningProvider, directory)
        {
            this.FileLastModified = DateTime.MinValue;
        }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public override string Name
        {
            get { return FilePath; }
        }

        public DateTime FileLastModified { get; set; }

        public override DateTime LastModified
        {
            get { return FileLastModified; }
        }

        public override long Length
        {
            get
            {
                return TextContents != null ?
                    TextContents.Length
                      : ByteContents != null ?
                    ByteContents.Length :
                    0;
            }
        }

        public string TextContents { get; set; }

        public byte[] ByteContents { get; set; }

        public override Stream OpenRead()
        {
            return MemoryStreamFactory.GetStream(ByteContents ?? (TextContents ?? "").ToUtf8Bytes());
        }
    }

}