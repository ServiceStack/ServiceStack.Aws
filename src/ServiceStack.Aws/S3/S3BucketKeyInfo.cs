using System;
using System.IO;
using Amazon.S3.Model;
using ServiceStack.Aws.Models;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.S3
{
    public class S3BucketKeyInfo : IEquatable<S3BucketKeyInfo>
    {
        public S3BucketKeyInfo(string bucketName, S3Object s3Object) : this(Path.Combine(bucketName, s3Object.Key)) { }

        public S3BucketKeyInfo(FileSystemObject fso) : this(fso.FullName) { }

        public S3BucketKeyInfo(string fullPathAndFileName, bool terminateWithPathDelimiter = false)
        {
            if (string.IsNullOrEmpty(fullPathAndFileName))
                throw new ArgumentNullException("fullPathAndFileName");

            Key = string.Empty;
            FileName = string.Empty;
            Prefix = string.Empty;

            fullPathAndFileName = fullPathAndFileName.Replace("\\", "/");

            if (terminateWithPathDelimiter && !fullPathAndFileName.EndsWith("/"))
            {
                fullPathAndFileName = string.Concat(fullPathAndFileName, "/");
            }

            var split = fullPathAndFileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            BucketName = split[0];

            if (split.Length > 1)
            {
                Key = string.Join("/", split, 1, split.Length - 1);

                if (fullPathAndFileName.EndsWith("/"))
                {
                    Key = Key + "/";
                    Prefix = Key;
                }
                else
                {
                    FileName = split[split.GetUpperBound(0)];

                    if (split.Length > 2)
                    {
                        Prefix = string.Join("/", split, 1, split.Length - 2) + "/";
                    }
                }
            }
            else
            {
                IsBucketObject = true;
            }
        }

        public string BucketName { get; private set; }
        public string Prefix { get; private set; }
        public string Key { get; private set; }
        public string FileName { get; private set; }

        public bool IsBucketObject { get; private set; }

        public bool HasPrefix
        {
            get { return !string.IsNullOrEmpty(Prefix); }
        }
        
        public bool Equals(S3BucketKeyInfo other)
        {
            return other != null &&
                   BucketName.Equals(other.BucketName, StringComparison.InvariantCultureIgnoreCase) &&
                   Key.Equals(other.Key, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            var fi = obj as S3BucketKeyInfo;

            return fi != null && Equals(fi);
        }

        public override string ToString()
        {
            return string.Concat(BucketName,
                BucketName.EndsWith("/")
                    ? string.Empty
                    : "/",
                Key);
        }
        
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

    }
}