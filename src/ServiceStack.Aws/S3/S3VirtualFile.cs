using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.S3
{
    public class S3VirtualFile : AbstractVirtualFileBase
    {
        private S3VirtualPathProvider PathProvider { get; set; }

        public IAmazonS3 Client
        {
            get { return PathProvider.Client; }
        }

        public string BucketName
        {
            get { return PathProvider.BucketName; }
        }

        public S3VirtualFile(S3VirtualPathProvider pathProvider, IVirtualDirectory directory)
            : base(pathProvider, directory)
        {
            this.PathProvider = pathProvider;
        }

        public string DirPath
        {
            get { return base.Directory.Name; }
        }

        public string FilePath { get; set; }

        public string FileName
        {
            get { return FilePath.SplitOnLast('/').Last(); }
        }

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
            get { return ContentLength; }
        }

        public long ContentLength { get; set; }

        public Stream Stream { get; set; }

        public S3VirtualFile Init(GetObjectResponse response)
        {
            FilePath = response.Key;
            ContentType = response.Headers.ContentType;
            FileLastModified = response.LastModified;
            ContentLength = response.Headers.ContentLength;
            Stream = response.ResponseStream;
            return this;
        }

        public override Stream OpenRead()
        {
            if (Stream == null)
            {
                var response = Client.GetObject(new GetObjectRequest
                {
                    Key = FilePath,
                    BucketName = BucketName,
                });
                Init(response);
            }

            return Stream;
        }
    }
}