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
    public partial class S3VirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public IAmazonS3 Client { get; private set; }
        public string BucketName { get; private set; }
        private readonly S3VirtualDirectory rootDirectory;

        public S3VirtualPathProvider(IAmazonS3 client, string bucketName, IAppHost appHost)
            : base(appHost)
        {
            this.Client = client;
            this.BucketName = bucketName;
            this.rootDirectory = new S3VirtualDirectory(this, null);
        }

        public const char DirSep = '/';

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

        protected override void Initialize() {}

        public override IVirtualFile GetFile(string virtualPath)
        {
            var filePath = SanitizePath(virtualPath);
            try
            {
                var response = Client.GetObject(new GetObjectRequest
                {
                    Key = filePath,
                    BucketName = BucketName,
                });

                return new S3VirtualFile(this, GetDirectory(GetDirPath(filePath))).Init(response);
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    return null;

                throw;
            }
        }

        public IVirtualDirectory GetDirectory(string dirPath)
        {
            return new S3VirtualDirectory(this, dirPath);
        }

        public void WriteFile(string filePath, string contents)
        {
            Client.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                ContentBody = contents,
            });
        }

        public void WriteFile(string filePath, Stream stream)
        {
            Client.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                InputStream = stream,
            });
        }

        public IEnumerable<S3VirtualFile> EnumerateFiles(string prefix)
        {
            var response = Client.ListObjects(new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
            });

            foreach (var file in response.S3Objects)
            {
                var filePath = SanitizePath(file.Key);
                yield return new S3VirtualFile(this, GetDirectory(GetDirPath(filePath)))
                {
                    FilePath = filePath,
                    ContentLength = file.Size,
                    FileLastModified = file.LastModified,
                };
            }
        }

        public IEnumerable<S3VirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = EnumerateFiles(fromDirPath)
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetSubDirPath(fromDirPath, x))
                .Where(x => x != null)
                .Distinct();

            return dirPaths.Map(x => new S3VirtualDirectory(this, x));
        }

        public IEnumerable<S3VirtualFile> GetImmediateFiles(string fromDirPath)
        {
            return EnumerateFiles(fromDirPath)
                .Where(x => x.DirPath == fromDirPath);
        }

        public string GetDirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            var lastDirPos = filePath.LastIndexOf(DirSep);
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public string GetSubDirPath(string fromDirPath, string subDirPath)
        {
            if (string.IsNullOrEmpty(subDirPath))
                return null;

            if (fromDirPath == null)
            {
                return subDirPath.CountOccurrencesOf(DirSep) == 0
                    ? subDirPath
                    : null;
            }

            if (!subDirPath.StartsWith(fromDirPath))
                return null;

            return fromDirPath.CountOccurrencesOf(DirSep) == subDirPath.CountOccurrencesOf(DirSep) - 1
                ? subDirPath
                : null;
        }

        public string SanitizePath(string filePath)
        {
            var sanitizedPath = string.IsNullOrEmpty(filePath)
                ? null
                : (filePath[0] == DirSep ? filePath.Substring(1) : filePath);

            return sanitizedPath != null
                ? sanitizedPath.Replace('\\', DirSep)
                : null;
        }
    }

    public partial class S3VirtualPathProvider : IS3Client
    {
        public const int MultiObjectLimit = 1000;

        public void ClearBucket()
        {
            var batches = EnumerateFiles(null)
                .BatchesOf(MultiObjectLimit);

            foreach (var batch in batches)
            {
                var request = new DeleteObjectsRequest
                {
                    BucketName = BucketName,
                };

                foreach (var file in batch)
                {
                    request.AddKey(file.FilePath);
                }

                Client.DeleteObjects(request);
            }

        }
    }
}