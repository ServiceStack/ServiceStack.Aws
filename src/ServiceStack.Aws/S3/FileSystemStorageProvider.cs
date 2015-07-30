using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Logging;

namespace ServiceStack.Aws.S3
{
    public class FileSystemStorageProvider : BaseFileStorageProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileSystemStorageProvider));

        public override void Download(FileSystemObject thisFso, FileSystemObject downloadToFso)
        {   // Download on FileSystem is just a copy operation
            Copy(thisFso, downloadToFso);
        }

        public override void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null)
        {
            // If targetProvider is null, copying within file system
            if (targetProvider == null)
            {
                CopyInFileSystem(thisFso, targetFso);
                return;
            }

            // If targetProvider is a FS provider, copying within file system
            var fsProvider = targetProvider as FileSystemStorageProvider;

            if (fsProvider != null)
            {
                CopyInFileSystem(thisFso, targetFso);
                return;
            }

            // Copying across providers (from local file system to some other file provider)
            targetProvider.Store(thisFso, targetFso);
        }

        private void CopyInFileSystem(FileSystemObject thisFso, FileSystemObject targetFso)
        {
            if (thisFso.Equals(targetFso))
            {
                return;
            }

            CreateFolder(targetFso.FolderName);
            File.Copy(thisFso.FullName, targetFso.FullName, overwrite: true);
        }

        public override void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso)
        {
            CreateFolder(targetMeta.FolderName);

            using(var sourceStream = new FileStream(sourceFileSystemMeta.FullPathAndFileName, FileMode.Open, FileAccess.Read))
            {
                SaveToDisk(sourceStream, targetMeta.FullPathAndFileName);
            }
        }

        public void Store(FileMetaData fileMetaData)
        {
            CreateFolder(fileMetaData.FolderName);

            using(var sourceStream = new MemoryStream(fileMetaData.Bytes, false))
            {
                SaveToDisk(sourceStream, fileMetaData.FullPathAndFileName);
            }
        }

        public Boolean Exists(FileMetaData fileMetaData)
        {
            return FileHelper.Exists(fileMetaData.FullPathAndFileName);
        }

        public void Move(String source, String target)
        {
            FileHelper.Move(source, target);
        }

        public void Delete(FileMetaData fileMetaData)
        {
            if (FileExists(fileMetaData.FullPathAndFileName))
            {
                FileHelper.Delete(fileMetaData.FullPathAndFileName);
            }
        }

        public Byte[] Get(FileMetaData fileMetaData)
        {
            return FileExists(fileMetaData.FullPathAndFileName)
                       ? FileHelper.ReadAllBytes(fileMetaData.FullPathAndFileName)
                       : null;
        }

        public void CreateFolder(String path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                PathHelper.Create(path);
            }
        }

        public void TryDeleteFolder(String path, Boolean recursive)
        {
            try
            {
                DeleteFolder(path, recursive);
            }
            catch(DirectoryNotFoundException)
            {
                // Ignore
            }
        }

        public void DeleteFolder(String path, Boolean recursive)
        {
            if (!String.IsNullOrEmpty(path))
            {
                PathHelper.Delete(path, recursive);
            }
        }

        public IEnumerable<String> ListFolder(String folderName, Boolean recursive = false)
        {
            return PathHelper.ListFolder(folderName, recursive);
        }

        private void SaveToDisk(Stream sourceStream, String targetFilePathAndName)
        {
            using(var fs = new FileStream(targetFilePathAndName, FileMode.Create, FileAccess.Write))
            {
                sourceStream.WriteTo(fs);
            }
        }

        private Boolean FileExists(String pathAndFileName)
        {
            return FileHelper.Exists(pathAndFileName);
        }
    }
}
