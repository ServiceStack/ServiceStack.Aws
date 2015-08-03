using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ServiceStack.Aws.Support
{
    public static class HashExtensions
    {
        public static string ToSha256HashString64(this string toHash, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(toHash))
            {
                return String.Empty;
            }
            if (encoding == null)
            {
                encoding = Encoding.Unicode;
            }

            var bytes = encoding.GetBytes(toHash).ToSha256ByteHash();
            return ToBase64String(bytes);
        }

        public static string ToBase64String(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public static byte[] ToSha256ByteHash(this byte[] byteBuffer)
        {
            if (byteBuffer == null || !byteBuffer.Any())
            {
                return null;
            }

            var ha = SHA256.Create();

            if (ha == null)
            {
                return null;
            }

            var hashValue = ha.ComputeHash(byteBuffer);
            ha.Clear();
            return hashValue;
        }

    }
}
