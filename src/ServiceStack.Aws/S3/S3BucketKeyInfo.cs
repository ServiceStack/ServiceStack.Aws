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
            Guard.Against(String.IsNullOrEmpty(fullPathAndFileName), "fullPathAndFileName must not be empty.");

            Key = String.Empty;
            FileName = String.Empty;
            Prefix = String.Empty;

            fullPathAndFileName = fullPathAndFileName.Replace("\\", "/");

            if (terminateWithPathDelimiter && !fullPathAndFileName.EndsWith("/"))
            {
                fullPathAndFileName = String.Concat(fullPathAndFileName, "/");
            }

            var split = fullPathAndFileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            BucketName = split[0];

            if (split.Length > 1)
            {
                Key = String.Join("/", split, 1, split.Length - 1);

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
                        Prefix = String.Join("/", split, 1, split.Length - 2) + "/";
                    }
                }
            }
            else
            {
                IsBucketObject = true;
            }
        }

        public String BucketName { get; private set; }
        public String Prefix { get; private set; }
        public String Key { get; private set; }
        public String FileName { get; private set; }

        public Boolean IsBucketObject { get; private set; }

        public Boolean HasPrefix
        {
            get { return !String.IsNullOrEmpty(Prefix); }
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
            return String.Concat(BucketName,
                                 BucketName.EndsWith("/")
                                     ? String.Empty
                                     : "/",
                                 Key);
        }
        
        public override Int32 GetHashCode()
        {
            return ToString().GetHashCode();
        }

    }
}