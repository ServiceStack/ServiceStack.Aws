using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.Aws.DynamoDb
{
    public interface IDynamoCommonQuery
    {
        string ProjectionExpression { get; set; }
        Dictionary<string, string> ExpressionAttributeNames { get; }
    }

    internal static class DynamoCommonQueryExtensions
    {
        internal static void SelectFields(this IDynamoCommonQuery q, IEnumerable<string> fields)
        {
            var fieldLabels = fields.Select(field => GetFieldLabel(q, field)).ToArray();
            q.ProjectionExpression = fieldLabels.Length > 0
                ? string.Join(", ", fieldLabels)
                : null;
        }

        internal static string GetFieldLabel(IDynamoCommonQuery q, string field)
        {
            if (!DynamoConfig.IsReservedWord(field))
                return field;

            var alias = "#" + field.Substring(0, 2).ToUpper();
            bool aliasExists = false;

            foreach (var entry in q.ExpressionAttributeNames)
            {
                if (entry.Value == field)
                    return entry.Key;

                if (entry.Key == alias)
                    aliasExists = true;
            }

            if (aliasExists)
                alias += q.ExpressionAttributeNames.Count;

            q.ExpressionAttributeNames[alias] = field;
            return alias;
        }
    }
}