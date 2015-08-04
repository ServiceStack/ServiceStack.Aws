using System;
using System.Globalization;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Text.Common;

namespace ServiceStack.Aws.DynamoDb
{
    public interface IAttributeValueConverter
    {
        AttributeValue ToAttributeValue(object value);
        object FromAttributeValue(AttributeValue attrValue);
    }

    public class DateTimeConverter : IAttributeValueConverter
    {
        public virtual AttributeValue ToAttributeValue(object value)
        {
            var iso8601Date = ((DateTime)value).ToString("o", CultureInfo.InvariantCulture);
            return new AttributeValue { S = iso8601Date };
        }

        public virtual object FromAttributeValue(AttributeValue attrValue)
        {
            var iso8601String = attrValue.S;
            var date = iso8601String == null
                ? null
                : DateTimeSerializer.ParseManual(iso8601String, DateTimeKind.Utc);

            return date;
        }
    }
}