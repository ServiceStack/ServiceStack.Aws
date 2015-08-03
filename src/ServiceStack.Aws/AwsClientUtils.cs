using System;
using ServiceStack.Text;

namespace ServiceStack.Aws
{
    internal static class AwsClientUtils
    {
        private class AccessToken
        {
            private string token;
            internal static readonly AccessToken __accessToken =
                new AccessToken("lUjBZNG56eE9yd3FQdVFSTy9qeGl5dlI5RmZwamc4U05udl000");
            private AccessToken(string token)
            {
                this.token = token;
            }
        }

        internal static IDisposable __requestAccess()
        {
            return LicenseUtils.RequestAccess(AccessToken.__accessToken, LicenseFeature.Client, LicenseFeature.Text);
        }

        internal static T FromJson<T>(string json)
        {
            using (__requestAccess())
            {
                return json.FromJson<T>();
            }
        }

        internal static string ToJson<T>(T o)
        {
            using (__requestAccess())
            {
                return o.ToJson();
            }
        }

        internal static JsConfigScope GetJsScope()
        {
            return JsConfig.With(excludeTypeInfo: false);
        }

        internal static string ToScopedJson<T>(T value)
        {
            using (GetJsScope())
            {
                return JsonSerializer.SerializeToString(value);
            }
        }
    }
}