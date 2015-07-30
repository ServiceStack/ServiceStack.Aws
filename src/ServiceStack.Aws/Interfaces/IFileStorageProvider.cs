using System;
using System.Collections.Generic;
using ServiceStack.Aws.S3;

namespace ServiceStack.Aws.Interfaces
{
    public interface IFileStorageProvider
    {
        void Download(string thisFileName, string localFileSystemTargetFileName);
        void Download(FileSystemObject thisFso, FileSystemObject localFileSystemFso);

        Byte[] Get(string thisFileName);
        Byte[] Get(FileSystemObject fso);

        void Store(Byte[] bytes, string targetFileName);
        void Store(Byte[] bytes, FileSystemObject fso);
        void Store(string localFileSystemSourceFileName, string targetFileName);
        void Store(FileSystemObject localFileSystemFso, FileSystemObject targetFso);

        void Delete(string fileName);
        void Delete(FileSystemObject fso);
        void Delete(IEnumerable<string> fileNames);
        void Delete(IEnumerable<FileSystemObject> fsos);

        void Copy(string thisFileName, string copyToFileName, IFileStorageProvider targetProvider = null);
        void Copy(FileSystemObject thisFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);

        void Move(string thisFileName, string moveToFileName, IFileStorageProvider targetProvider = null);
        void Move(FileSystemObject sourceFso, FileSystemObject targetFso, IFileStorageProvider targetProvider = null);

        bool Exists(string fileName);
        bool Exists(FileSystemObject fso);

        IEnumerable<string> ListFolder(string folderName, Boolean recursive = false);
        void DeleteFolder(string folderName, Boolean recursive);
        void CreateFolder(string folderName);
    }
}