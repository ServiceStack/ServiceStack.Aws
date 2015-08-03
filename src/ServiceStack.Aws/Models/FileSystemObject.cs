using System;
using System.Globalization;
using System.IO;

namespace ServiceStack.Aws.Models
{
    public class FileSystemObject : ICloneable, IEquatable<FileSystemObject>
    {
        #region ICloneable
        object ICloneable.Clone()
        {
            return Clone();
        }

        public FileSystemObject Clone()
        {
            var cloned = (FileSystemObject)MemberwiseClone();
            return cloned;
        }
        #endregion ICloneable

        private readonly string _directorySeparatorCharacter;

        public FileSystemObject() { }

        public FileSystemObject(string path, string fileName) : this(Path.Combine(path ?? String.Empty, fileName)) { }

        public FileSystemObject(string filePathAndName)
        {   // Figure out if there are mixed directory markers and adjust to the appropriate one across the board - If we have 
            // both path separators, use only the first. If we have only one, use that
            var slashPartIndex = filePathAndName.IndexOf("/", StringComparison.InvariantCultureIgnoreCase);
            var backslashPartIndex = filePathAndName.IndexOf("\\", StringComparison.InvariantCultureIgnoreCase);

            var useBackslashDirectorySeparator = (slashPartIndex >= 0 && backslashPartIndex >= 0)
                                                     ? backslashPartIndex < slashPartIndex
                                                     : backslashPartIndex >= 0;

            // Set the character separator to use
            _directorySeparatorCharacter = useBackslashDirectorySeparator
                                               ? "\\"
                                               : "/";

            Init(filePathAndName);
        }

        public FileSystemObject(string filePathAndName, char directorySeparatorCharacter)
        {
            _directorySeparatorCharacter = directorySeparatorCharacter.ToString(CultureInfo.InvariantCulture);
            Init(filePathAndName);
        }

        private void Init(string filePathAndName)
        {
            if (filePathAndName.EndsWith("\\") || filePathAndName.EndsWith("/"))
            {   // Ends with a path marker - check to see if this is actually a FILE ending with a terminator - if so, remove it
                var isFileTestPath = filePathAndName.TrimEnd(new[] { '\\', '/' });

                var isFileTestExtension = Path.GetExtension(isFileTestPath);

                if (!String.IsNullOrEmpty(isFileTestExtension) &&
                    isFileTestPath.EndsWith(isFileTestExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    filePathAndName = isFileTestPath;
                }
            }

            Func<string, string> pathScrubber = (f) => f.Replace(_directorySeparatorCharacter.Equals("\\", StringComparison.InvariantCulture)
                                                                     ? "/"
                                                                     : "\\",
                                                                 _directorySeparatorCharacter.Equals("\\", StringComparison.InvariantCulture)
                                                                     ? "\\"
                                                                     : "/");

            FileName = pathScrubber(Path.GetFileNameWithoutExtension(filePathAndName));
            FolderName = pathScrubber(Path.GetDirectoryName(filePathAndName));

            var fileExtension = pathScrubber(Path.GetExtension(filePathAndName));

            FileExtension = fileExtension.StartsWith(".", StringComparison.InvariantCultureIgnoreCase)
                                ? fileExtension.Substring(1)
                                : fileExtension;
        }

        public string FileName { get; private set; }
        public string FolderName { get; private set; }
        public string FileExtension { get; private set; }

        private string Combine(params string[] paths)
        {
            var appendSeparator = false;
            var returnVal = String.Empty;

            foreach (var path in paths)
            {
                if (String.IsNullOrEmpty(path))
                {
                    continue;
                }

                returnVal = String.Concat(returnVal,
                                          appendSeparator
                                              ? _directorySeparatorCharacter
                                              : String.Empty,
                                          path);
                
                appendSeparator = !path.EndsWith(_directorySeparatorCharacter, StringComparison.InvariantCultureIgnoreCase);
            }

            return returnVal;
        }

        public string FullName
        {
            get
            {
                return Combine(String.IsNullOrEmpty(FolderName) ? String.Empty : FolderName, FileNameAndExtension);
            }
        }

        public String FileNameAndExtension
        {
            get
            {
                return String.IsNullOrEmpty(FileExtension)
                           ? FileName
                           : String.Concat(FileName, ".", FileExtension);
            }
        }
        
        public bool Equals(FileSystemObject other)
        {   // Purpsely always use case-insensitive comparison for our purposes, seems safest to me
            return other != null &&
                   FullName.Equals(other.FullName, StringComparison.InvariantCulture);
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

            var fsoObject = obj as FileSystemObject;

            return fsoObject != null && Equals(fsoObject);
        }

        public override string ToString()
        {
            return FullName;
        }
        
        public override Int32 GetHashCode()
        {
            return ToString().GetHashCode();
        }
        
    }
}