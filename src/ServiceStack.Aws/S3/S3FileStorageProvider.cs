//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Amazon.S3;
//using Amazon.S3.IO;
//using Amazon.S3.Model;
//using ServiceStack.Aws.Interfaces;
//using ServiceStack.Aws.Support;
//using ServiceStack.Logging;

//namespace ServiceStack.Aws.S3
//{
//    public class S3FileStorageProvider : IFileStorageProvider, IDisposable
//    {
//        private static readonly ILog _log = LogManager.GetLogger(typeof(S3FileStorageProvider));
//        private readonly S3ConnectionFactory _s3ConnectionFactory;
//        private IAmazonS3 _s3Client;

//        public S3FileStorageProvider(S3ConnectionFactory s3ConnectionFactory)
//        {
//            Guard.AgainstNullArgument(s3ConnectionFactory, "s3ConnectionFactory");

//            _s3ConnectionFactory = s3ConnectionFactory;
//        }

//        public void Dispose()
//        {
//            if (_s3Client == null)
//            {
//                return;
//            }

//            try
//            {
//                _s3Client.Dispose();
//                _s3Client = null;
//            }
//            catch { }
//        }

//        public IAmazonS3 S3Client
//        {
//            get { return _s3Client ?? (_s3Client = _s3ConnectionFactory.GetClient()); }
//        }

//        public void Download(string from, string to)
//        {
//            var path = new S3DirectoryInfo()
//            PathHelper.Create(downloadToMeta.FolderName);
//            GetToFile(thisProviderMeta, downloadToMeta.FullPathAndFileName);
//        }

//        public void CopyTo(FileMetaData thisProviderMeta, IFileStorageProvider targetProvider, FileMetaData targetMeta)
//        {   // Store method with multiple FileMeta objects assumes the source is on the filesystem (or handles that case)
//            // so with an S3 source provider, need to copy the given source to the local filesystem first, use that
//            // as the source for the given target provider, then cleanup naturally
//            var localFile = Path.Combine(Path.GetTempPath(), targetMeta.FileName);
//            FileHelper.Delete(localFile);

//            try
//            {
//                GetToFile(thisProviderMeta, localFile);

//                var localFileSystemMeta = new FileMetaData(localFile);
//                targetProvider.Store(localFileSystemMeta, targetMeta);
//            }
//            finally
//            {
//                FileHelper.Delete(localFile);
//            }
//        }

//        private void GetToFile(FileMetaData s3Meta, String fileNameToCreate)
//        {
//            var bpk = new BucketPrefixKey(s3Meta.FullPathAndFileName);

//            var request = new GetObjectRequest
//            {
//                BucketName = bpk.BucketName,
//                Key = bpk.Key
//            };

//            FileHelper.Delete(fileNameToCreate);

//            var awsClient = Connect();

//            using (var response = awsClient.GetObject(request))
//            {
//                response.WriteResponseStreamToFile(fileNameToCreate);
//            }

//        }

//        public void Store(FileMetaData sourceFileSystemMeta, FileMetaData targetMeta)
//        {
//            PostToS3(targetMeta, sourceFileSystemMeta.FullPathAndFileName);
//        }

//        public void Store(FileMetaData fileMetaData)
//        {
//            PostToS3(fileMetaData, null);
//        }

//        public void CreateFolder(String folderName)
//        {
//            var bpk = new BucketPrefixKey(folderName);

//            var awsClient = Connect();

//            var request = new PutBucketRequest
//            {
//                BucketName = bpk.BucketName,
//                UseClientRegion = true
//            };

//            var response = awsClient.PutBucket(request);
//        }

//        private void PostToS3(FileMetaData targetMetaData, String sourceFilePathAndName = null)
//        {
//            var attemptedBucketCreate = false;

//            do
//            {
//                try
//                {
//                    if (String.IsNullOrEmpty(sourceFilePathAndName))
//                    {
//                        using (var byteStream = new MemoryStream(targetMetaData.Bytes))
//                        {
//                            PostToS3WithStream(targetMetaData, byteStream);
//                        }
//                    }
//                    else
//                    {
//                        using (var stream = new FileStream(sourceFilePathAndName, FileMode.Open, FileAccess.Read))
//                        {
//                            PostToS3WithStream(targetMetaData, stream);
//                        }
//                    }

//                    break;
//                }
//                catch (AmazonS3Exception s3x)
//                {
//                    if (!attemptedBucketCreate && s3x.ErrorCode == AmazonS3ErrorCodes.NoSuchBucket)
//                    {
//                        CreateFolder(targetMetaData.FolderName);
//                        attemptedBucketCreate = true;
//                        continue;
//                    }

//                    throw;
//                }

//            } while (true);
//        }

//        private void PostToS3WithStream(FileMetaData fileMetaData, Stream streamToUse)
//        {
//            var bpk = new BucketPrefixKey(fileMetaData.FullPathAndFileName);

//            var awsClient = Connect();

//            var request = new PutObjectRequest
//            {
//                BucketName = bpk.BucketName,
//                Key = bpk.Key,
//                InputStream = streamToUse,
//                StorageClass = VengaEnvironment.IsDevelopmentEnvironment()
//                                   ? S3StorageClass.ReducedRedundancy
//                                   : S3StorageClass.Standard
//            };

//            if (!String.IsNullOrEmpty(fileMetaData.DisplayName))
//            {
//                request.Metadata.Add("file-display-name", fileMetaData.DisplayName);
//            }

//            var response = awsClient.PutObject(request);
//        }

//        public void TryDeleteFolder(String folderName, Boolean recursive)
//        {
//            try
//            {
//                DeleteFolder(folderName, recursive);
//            }
//            catch (AmazonS3Exception s3x)
//            {
//                if ((s3x.ErrorCode != AmazonS3ErrorCodes.NoSuchBucket) &&
//                    (s3x.ErrorCode != AmazonS3ErrorCodes.NoSuchKey))
//                {
//                    throw;
//                }
//            }
//        }

//        public void DeleteFolder(String folderName, Boolean recursive)
//        {
//            var bpk = new BucketPrefixKey(folderName, terminateWithPathDelimiter: true);

//            var awsClient = Connect();

//            if (recursive)
//            {
//                while (true)
//                {
//                    var objects = ListFolder(bpk);

//                    if (!objects.Any())
//                    {
//                        break;
//                    }

//                    var keys = objects.Select(o => new KeyVersion { Key = o.Key }).ToList();

//                    var deleteObjectsRequest = new DeleteObjectsRequest
//                    {
//                        BucketName = bpk.BucketName,
//                        Quiet = true,
//                        Objects = keys
//                    };

//                    awsClient.DeleteObjects(deleteObjectsRequest);
//                }
//            }
//            else if (!bpk.IsBucketObject)
//            {
//                var deleteObjectRequest = new DeleteObjectRequest
//                {
//                    BucketName = bpk.BucketName,
//                    Key = bpk.Key
//                };
//                awsClient.DeleteObject(deleteObjectRequest);
//            }

//            if (bpk.IsBucketObject)
//            {
//                var request = new DeleteBucketRequest
//                {
//                    BucketName = bpk.BucketName,
//                    UseClientRegion = true
//                };

//                var response = awsClient.DeleteBucket(request);
//            }
//        }

//        public void Move(String source, String target)
//        {
//            var sourceBpk = new BucketPrefixKey(source);
//            var targetBpk = new BucketPrefixKey(target);

//            if (sourceBpk.Matches(targetBpk))
//            {
//                return;
//            }

//            var stored = false;

//            try
//            {
//                var copyRequest = new CopyObjectRequest()
//                {
//                    SourceBucket = sourceBpk.BucketName,
//                    SourceKey = sourceBpk.Key,
//                    DestinationBucket = targetBpk.BucketName,
//                    DestinationKey = targetBpk.Key
//                };

//                var awsClient = Connect();

//                awsClient.CopyObject(copyRequest);

//                stored = true;

//                Delete(sourceBpk);
//            }
//            catch (Exception)
//            {
//                if (stored)
//                {
//                    Delete(new FileMetaData(target));
//                }

//                throw;
//            }
//        }

//        public IEnumerable<String> ListFolder(String folderName, Boolean recursive = false)
//        {
//            String nextMarker = null;

//            var bpk = new BucketPrefixKey(folderName, terminateWithPathDelimiter: true);

//            do
//            {
//                var listResponse = ListFolderResponse(bpk, nextMarker);

//                if (listResponse == null || listResponse.S3Objects == null)
//                {
//                    break;
//                }

//                var filesOnly = listResponse.S3Objects
//                                            .Select(o => new BucketPrefixKey(bpk.BucketName, o))
//                                            .Where(b => !String.IsNullOrEmpty(b.FileName))
//                                            .Where(b => recursive
//                                                            ? b.Prefix.StartsWith(bpk.Prefix, StringComparison.InvariantCultureIgnoreCase)
//                                                            : Strings.EqualsInvariant(b.Prefix, bpk.Prefix))
//                                            .Select(b => b.FileName);

//                foreach (var file in filesOnly)
//                {
//                    yield return file;
//                }

//                if (listResponse.IsTruncated)
//                {
//                    nextMarker = listResponse.NextMarker;
//                }
//                else
//                {
//                    break;
//                }

//            } while (true);

//        }

//        private List<S3Object> ListFolder(BucketPrefixKey bpk)
//        {
//            var listResponse = ListFolderResponse(bpk);
//            return listResponse == null
//                       ? new List<S3Object>()
//                       : listResponse.S3Objects ?? new List<S3Object>();
//        }

//        private ListObjectsResponse ListFolderResponse(BucketPrefixKey bpk, String nextMarker = null)
//        {
//            var awsClient = Connect();

//            try
//            {
//                var listRequest = new ListObjectsRequest
//                {
//                    BucketName = bpk.BucketName
//                };

//                if (nextMarker.HasValue())
//                {
//                    listRequest.Marker = nextMarker;
//                }

//                if (bpk.HasPrefix)
//                {
//                    listRequest.Prefix = bpk.Prefix;
//                }

//                var listResponse = awsClient.ListObjects(listRequest);

//                return listResponse;
//            }
//            catch (System.Xml.XmlException)
//            {
//            }
//            catch (AmazonS3Exception s3x)
//            {
//                if ((s3x.ErrorCode != AmazonS3ErrorCodes.NoSuchBucket) &&
//                    (s3x.ErrorCode != AmazonS3ErrorCodes.NoSuchKey))
//                {
//                    throw;
//                }
//            }

//            return null;
//        }

//        public void Delete(FileMetaData fileMetaData)
//        {
//            var bpk = new BucketPrefixKey(fileMetaData.FullPathAndFileName);

//            Delete(bpk);
//        }

//        private void Delete(BucketPrefixKey file)
//        {
//            var awsClient = Connect();

//            var request = new DeleteObjectRequest
//            {
//                BucketName = file.BucketName,
//                Key = file.Key
//            };

//            var response = awsClient.DeleteObject(request);
//        }

//        public Boolean Exists(FileMetaData fileMetaData)
//        {
//            var bpk = new BucketPrefixKey(fileMetaData.FullPathAndFileName);

//            var s3FileInfo = new S3FileInfo(Connect(), bpk.BucketName, bpk.Key);

//            return s3FileInfo.Exists;
//        }

//        public Byte[] Get(FileMetaData fileMetaData)
//        {
//            var bpk = new BucketPrefixKey(fileMetaData.FullPathAndFileName);

//            var request = new GetObjectRequest
//            {
//                BucketName = bpk.BucketName,
//                Key = bpk.Key
//            };

//            try
//            {
//                var awsClient = Connect();

//                using (var response = awsClient.GetObject(request))
//                {
//                    return response.ResponseStream.ToBytes();
//                }
//            }
//            catch (AmazonS3Exception)
//            {
//                return null;
//            }

//        }

//    }

//    public class BucketPrefixKey : IMatchable<BucketPrefixKey>
//    {
//        public BucketPrefixKey(String bucketName, S3Object s3Object) : this(Path.Combine(bucketName, s3Object.Key), false) { }

//        public BucketPrefixKey(String fullPathAndFileName) : this(fullPathAndFileName, false) { }

//        public BucketPrefixKey(String fullPathAndFileName, Boolean terminateWithPathDelimiter)
//        {
//            Guard.Against(String.IsNullOrEmpty(fullPathAndFileName), "fullPathAndFileName must not be empty.");

//            Key = String.Empty;
//            FileName = String.Empty;
//            Prefix = String.Empty;

//            fullPathAndFileName = fullPathAndFileName.Replace("\\", "/");

//            if (terminateWithPathDelimiter && !fullPathAndFileName.EndsWith("/"))
//            {
//                fullPathAndFileName = String.Concat(fullPathAndFileName, "/");
//            }

//            var split = fullPathAndFileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
//            BucketName = split[0];

//            if (split.Length > 1)
//            {
//                Key = String.Join("/", split, 1, split.Length - 1);

//                if (fullPathAndFileName.EndsWith("/"))
//                {
//                    Key = Key + "/";
//                    Prefix = Key;
//                }
//                else
//                {
//                    FileName = split[split.GetUpperBound(0)];

//                    if (split.Length > 2)
//                    {
//                        Prefix = String.Join("/", split, 1, split.Length - 2) + "/";
//                    }
//                }
//            }
//            else
//            {
//                IsBucketObject = true;
//            }
//        }

//        public String BucketName { get; private set; }
//        public String Prefix { get; private set; }
//        public String Key { get; private set; }
//        public String FileName { get; private set; }

//        public Boolean IsBucketObject { get; private set; }

//        public Boolean HasPrefix
//        {
//            get { return !String.IsNullOrEmpty(Prefix); }
//        }

//        public Boolean Matches(BucketPrefixKey that)
//        {
//            return BucketName.EqualsInvariant(that.BucketName) && Key.EqualsInvariant(that.Key);
//        }

//    }
//}
