// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Threading;
using ServiceStack.Text;

namespace ServiceStack.Aws.Support
{
    internal static class AwsClientUtils
    {
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

        internal static void SleepBackOffMultiplier(this int i)
        {
            var nextTryMs = (2 ^ i) * 50;
            Thread.Sleep(nextTryMs);
        }
    }
}