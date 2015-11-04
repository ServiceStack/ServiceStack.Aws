// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Linq.Expressions;
using System.Threading;
using ServiceStack.Text;

namespace ServiceStack.Aws.Support
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

        internal static string GetMemberName<T>(Expression<Func<T, object>> fieldExpr)
        {
            var m = GetMemberExpression(fieldExpr);
            if (m != null)
                return m.Member.Name;

            throw new NotSupportedException("Expected Property Expression");
        }

        private static MemberExpression GetMemberExpression<T>(Expression<Func<T, object>> expr)
        {
            var member = expr.Body as MemberExpression;
            var unary = expr.Body as UnaryExpression;
            return member ?? (unary != null ? unary.Operand as MemberExpression : null);
        }

        internal static void SleepBackOffMultiplier(this int i)
        {
            var nextTryMs = (2 ^ i) * 50;
            Thread.Sleep(nextTryMs);
        }
    }
}