using System;
using System.Collections.Generic;
using ServiceStack.Aws.Interfaces;

namespace ServiceStack.Aws.S3
{
    public abstract class BaseFileStorageProvider : IFileStorageProvider
    {
        protected BaseFileStorageProvider() { }

        public void Download(string thisFileName, string localFileSystemTargetFileName)
        {
            var sourceFso = new FileSystemObject(thisFileName);
            var targetFso = new FileSystemObject(localFileSystemTargetFileName);
            Download(sourceFso, targetFso);
        }

        public Byte[] Get(string thisFileName)
        {
            var fso = new FileSystemObject(thisFileName);
            return Get(fso);
        }

        public void Store(Byte[] bytes, string targetFileName)
        {
            var fso = new FileSystemObject(targetFileName);
            Store(bytes, fso);
        }

        public void Store(string localFileSystemSourceFileName, string targetFileName)
        {
            var localFileSystemFso = new FileSystemObject(localFileSystemSourceFileName);
            var fso = new FileSystemObject(targetFileName);
            Store(localFileSystemFso, fso);
        }

        public void Delete(string fileName)
        {
            var fso = new FileSystemObject(fileName);
            Delete(fso);
        }

        public void Delete(IEnumerable<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Delete(fileName);
            }
        }

        public void Copy(string thisFileName, string copyToFileName, IFileStorageProvider targetProvider = null)
        {
            var sourceFso = new FileSystemObject(thisFileName);
            var targetFso = new FileSystemObject(copyToFileName);
            Copy(sourceFso, targetFso, targetProvider);
        }

        public void Move(string thisFileName, string moveToFileName, IFileStorageProvider targetProvider = null)
        {
            var sourceFso = new FileSystemObject(thisFileName);
            var targetFso = new FileSystemObject(moveToFileName);
            Move(sourceFso, targetFso, targetProvider);
        }

        public bool Exists(string fileName)
        {
            var fso = new FileSystemObject(fileName);
            return Exists(fso);
        }

        public abstract void Download(FileSystemObject thisFso, FileSystemObject localFileSystemFso);
        public abstract byte[] Get(FileSystemObject fso);
        public abstract void Store(Byte[] bytes, FileSystemObject fso);
        public abstract void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso);
        public abstract void Delete(FileSystemObject fso);
        public abstract void Delete(IEnumerable<FileSystemObject> fsos);
        public abstract void Copy(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);
        public abstract void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);
        public abstract bool Exists(FileSystemObject fso);
        public abstract IEnumerable<string> ListFolder(string folderName, bool recursive = false);
        public abstract void DeleteFolder(string folderName, bool recursive);
        public abstract void CreateFolder(string folderName);
    }
}
