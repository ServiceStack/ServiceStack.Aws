using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.S3
{
    public class S3VirtualDirectory : AbstractVirtualDirectoryBase
    {
        internal S3VirtualPathProvider PathProvider { get; private set; }

        public S3VirtualDirectory(S3VirtualPathProvider pathProvider, string dirPath)
            : base(pathProvider)
        {
            this.PathProvider = pathProvider;
            this.DirPath = dirPath;
        }
        
        static readonly char DirSep = '/';

        public DateTime DirLastModified { get; set; }

        public override DateTime LastModified
        {
            get { return DirLastModified; }
        }

        public override IEnumerable<IVirtualFile> Files
        {
            get { return PathProvider.GetImmediateFiles(DirPath); }
        }

        public override IEnumerable<IVirtualDirectory> Directories
        {
            get { return PathProvider.GetImmediateDirectories(DirPath); }
        }

        public IAmazonS3 Client
        {
            get { return PathProvider.Client; }
        }

        public string BucketName
        {
            get { return PathProvider.BucketName; }
        }

        public string DirPath { get; set; }

        public override string VirtualPath
        {
            get { return DirPath; }
        }

        public override string Name
        {
            get { return DirPath != null ? DirPath.SplitOnLast(InMemoryVirtualPathProvider.DirSep).Last() : null; }
        }

        public override IVirtualFile GetFile(string virtualPath)
        {
            try
            {
                var response = Client.GetObject(new GetObjectRequest
                {
                    Key = DirPath.CombineWith(virtualPath),
                    BucketName = BucketName,                    
                });

                return new S3VirtualFile(PathProvider, this).Init(response);
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
            var matchingFilesInBackingDir = EnumerateFiles(globPattern);
            return matchingFilesInBackingDir;
        }

        public IEnumerable<S3VirtualFile> EnumerateFiles(string pattern)
        {
            foreach (var file in PathProvider.GetImmediateFiles(DirPath).Where(f => f.Name.Glob(pattern)))
            {
                yield return file;
            }
        }

        protected override IVirtualDirectory GetDirectoryFromBackingDirectoryOrDefault(string directoryName)
        {
            return new S3VirtualDirectory(PathProvider, PathProvider.SanitizePath(DirPath.CombineWith(directoryName)));
        }

        public void AddFile(string filePath, string contents)
        {
            Client.PutObject(new PutObjectRequest
            {
                Key = StripDirSeparatorPrefix(filePath),
                BucketName = PathProvider.BucketName,
                ContentBody = contents,
            });
        }

        public void AddFile(string filePath, Stream stream)
        {
            Client.PutObject(new PutObjectRequest
            {
                Key = StripDirSeparatorPrefix(filePath),
                BucketName = PathProvider.BucketName,
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
}