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
    public partial class S3VirtualPathProvider : AbstractVirtualPathProviderBase, IWriteableVirtualPathProvider
    {
        public const int MultiObjectLimit = 1000;

        public IAmazonS3 AmazonS3 { get; private set; }
        public string BucketName { get; private set; }
        private readonly S3VirtualDirectory rootDirectory;

        public S3VirtualPathProvider(IAmazonS3 client, string bucketName, IAppHost appHost)
            : base(appHost)
        {
            this.AmazonS3 = client;
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
                var response = AmazonS3.GetObject(new GetObjectRequest
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
            AmazonS3.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                ContentBody = contents,
            });
        }

        public void WriteFile(string filePath, Stream stream)
        {
            AmazonS3.PutObject(new PutObjectRequest
            {
                Key = SanitizePath(filePath),
                BucketName = BucketName,
                InputStream = stream,
            });
        }

        public void WriteFiles(IEnumerable<IVirtualFile> files, Func<IVirtualFile, string> toPath = null)
        {
            this.CopyFrom(files, toPath);
        }

        public void DeleteFile(string filePath)
        {
            filePath = SanitizePath(filePath);
            AmazonS3.DeleteObject(new DeleteObjectRequest {
                BucketName = BucketName,
                Key = filePath,
            });
        }

        public void DeleteFiles(IEnumerable<string> filePaths)
        {
            var batches = filePaths
                .BatchesOf(MultiObjectLimit);

            foreach (var batch in batches)
            {
                var request = new DeleteObjectsRequest {
                    BucketName = BucketName,
                };

                foreach (var filePath in batch)
                {
                    request.AddKey(filePath);
                }

                AmazonS3.DeleteObjects(request);
            }
        }

        public void DeleteFolder(string dirPath)
        {
            dirPath = SanitizePath(dirPath);
            var nestedFiles = EnumerateFiles(dirPath).Map(x => x.FilePath);
            DeleteFiles(nestedFiles);
        }

        public IEnumerable<S3VirtualFile> EnumerateFiles(string prefix = null)
        {
            var response = AmazonS3.ListObjects(new ListObjectsRequest
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

        public override IEnumerable<IVirtualFile> GetAllFiles()
        {
            return EnumerateFiles();
        }

        public IEnumerable<S3VirtualDirectory> GetImmediateDirectories(string fromDirPath)
        {
            var dirPaths = EnumerateFiles(fromDirPath)
                .Map(x => x.DirPath)
                .Distinct()
                .Map(x => GetImmediateSubDirPath(fromDirPath, x))
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

        public string GetImmediateSubDirPath(string fromDirPath, string subDirPath)
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

        public static string GetFileName(string filePath)
        {
            return filePath.SplitOnLast(DirSep).Last();
        }
    }

    public partial class S3VirtualPathProvider : IS3Client
    {
        public void ClearBucket()
        {
            var allFilePaths = EnumerateFiles()
                .Map(x => x.FilePath);

            DeleteFiles(allFilePaths);
        }
    }
}