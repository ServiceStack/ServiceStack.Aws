using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.S3
{
    public class S3VirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public IAmazonS3 Client { get; private set; }
        public string BucketName { get; private set; }

        public S3VirtualPathProvider(IAmazonS3 client, string bucketName, IAppHost appHost)
            : base(appHost)
        {
            this.Client = client;
            this.BucketName = bucketName;
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

        public void AddFile(string filePath, Stream stream)
        {
            rootDirectory.AddFile(filePath, stream);
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            return rootDirectory.GetFile(virtualPath)
                ?? base.GetFile(virtualPath);
        }
    }

    public class S3VirtualDirectory : AbstractVirtualDirectoryBase
    {
        private S3VirtualPathProvider pathProvider;

        public S3VirtualDirectory(S3VirtualPathProvider pathProvider)
            : base(pathProvider)
        {
            this.pathProvider = pathProvider;
            this.files = new List<S3VirtualFile>();
            this.dirs = new List<S3VirtualDirectory>();
            this.DirLastModified = DateTime.MinValue;
        }

        public S3VirtualDirectory(IVirtualPathProvider owningProvider, IVirtualDirectory parentDirectory)
            : base(owningProvider, parentDirectory) { }

        static readonly char DirSep = '/';

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

        public IAmazonS3 Client
        {
            get { return pathProvider.Client; }
        }

        public string BucketName
        {
            get { return pathProvider.BucketName; }
        }

        public string DirName { get; set; }
        public override string Name
        {
            get { return DirName; }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            try
            {
                var response = Client.GetObject(new GetObjectRequest
                {
                    Key = StripDirSeparatorPrefix(virtualPath),
                    BucketName = BucketName,
                });

                return new S3VirtualFile(pathProvider, this)
                {
                    FileName = response.Key.SplitOnLast('/').Last(),
                    FilePath = response.Key,
                    ContentType = response.Headers.ContentType,
                    FileLastModified = response.LastModified,
                    Stream = response.ResponseStream,
                };
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return null;

                throw;
            }
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

        public void AddFile(string filePath, string contents)
        {
            Client.PutObject(new PutObjectRequest {
                Key = StripDirSeparatorPrefix(filePath),
                BucketName = pathProvider.BucketName,
                ContentBody = contents,
            });
        }

        public void AddFile(string filePath, Stream stream)
        {
            Client.PutObject(new PutObjectRequest {
                Key = StripDirSeparatorPrefix(filePath),
                BucketName = pathProvider.BucketName,
                InputStream = stream,
            });
        }

        private static string StripDirSeparatorPrefix(string filePath)
        {
            return string.IsNullOrEmpty(filePath)
                ? filePath
                : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);
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

        public string ContentType { get; set; }

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

        private string textContents;
        public string TextContents
        {
            get { return textContents ?? (textContents = ReadAllText()); }
        }

        private byte[] byteContents;
        public byte[] ByteContents
        {
            get { return byteContents ?? (byteContents = OpenRead().ReadFully()); }
        }

        public Stream Stream { get; set; }

        public override Stream OpenRead()
        {
            return Stream;
        }
    }

}