using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public class PocoDynamoExpression
    {
        public static Func<Type, Expression, PocoDynamoExpression> FactoryFn =
            (type, expr) => new PocoDynamoExpression(type).Parse(expr);

        private static string sep = " ";

        public Type Type { get; set; }
        public DynamoMetadataType DbType { get; set; }

        public string FilterExpression { get; set; }

        public Dictionary<string, object> Params { get; set; }

        public PocoDynamoExpression(Type type)
        {
            Type = type;
            DbType = DynamoMetadata.GetType(type);
            Params = new Dictionary<string, object>();
        }

        public PocoDynamoExpression Parse(Expression expr)
        {
            FilterExpression = Visit(expr).ToString();
            return this;
        }

        protected internal virtual object Visit(Expression exp)
        {
            VisitedExpressionIsTableColumn = false;

            if (exp == null) return string.Empty;
            switch (exp.NodeType)
            {
                case ExpressionType.Lambda:
                    return VisitLambda(exp as LambdaExpression);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess(exp as MemberExpression);
                case ExpressionType.Constant:
                    return VisitConstant(exp as ConstantExpression);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    return VisitBinary(exp as BinaryExpression);
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary(exp as UnaryExpression);
                case ExpressionType.Parameter:
                    return VisitParameter(exp as ParameterExpression);
                case ExpressionType.Call:
                    return VisitMethodCall(exp as MethodCallExpression);
                case ExpressionType.New:
                    return VisitNew(exp as NewExpression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray(exp as NewArrayExpression);
                case ExpressionType.MemberInit:
                    return VisitMemberInit(exp as MemberInitExpression);
                default:
                    return exp.ToString();
            }
        }

        protected virtual object VisitParameter(ParameterExpression p)
        {
            return p.Name;
        }

        protected virtual object VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Dynamo))
                return VisitSqlMethodCall(m);

            if (IsStaticArrayMethod(m))
                return VisitStaticArrayMethodCall(m);

            if (IsEnumerableMethod(m))
                return VisitEnumerableMethodCall(m);

            if (IsColumnAccess(m))
                return VisitColumnAccessMethod(m);

            return Expression.Lambda(m).Compile().DynamicInvoke();
        }

        protected virtual object VisitSqlMethodCall(MethodCallExpression m)
        {
            List<object> args = this.VisitExpressionList(m.Arguments);
            object quotedColName = args[0];
            args.RemoveAt(0);

            string statement;

            switch (m.Method.Name)
            {
                default:
                    var dynamoName = m.Method.Name.ToLowercaseUnderscore();
                    statement = string.Format("{0}({1}{2})",
                        dynamoName,
                        quotedColName,
                        args.Count == 1 ? string.Format(", {0}", GetValueAsParam(args[0])) : "");
                    break;
            }

            return new PartialString(statement);
        }

        protected virtual List<object> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            var list = new List<object>();
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var e = original[i];
                if (e.NodeType == ExpressionType.NewArrayInit ||
                    e.NodeType == ExpressionType.NewArrayBounds)
                {
                    list.AddRange(VisitNewArrayFromExpressionList(e as NewArrayExpression));
                }
                else
                {
                    list.Add(Visit(e));
                }
            }
            return list;
        }

        //protected virtual List<object> VisitInSqlExpressionList(ReadOnlyCollection<Expression> original)
        //{
        //    var list = new List<object>();
        //    for (int i = 0, n = original.Count; i < n; i++)
        //    {
        //        var e = original[i];
        //        if (e.NodeType == ExpressionType.NewArrayInit ||
        //            e.NodeType == ExpressionType.NewArrayBounds)
        //        {
        //            list.AddRange(VisitNewArrayFromExpressionList(e as NewArrayExpression));
        //        }
        //        else if (e.NodeType == ExpressionType.MemberAccess)
        //        {
        //            list.Add(GetMemberExpression(e as MemberExpression));
        //        }
        //        else
        //        {
        //            list.Add(Visit(e));
        //        }
        //    }
        //    return list;
        //}

        private bool IsStaticArrayMethod(MethodCallExpression m)
        {
            if (m.Object == null && m.Method.Name == "Contains")
                return m.Arguments.Count == 2;

            return false;
        }

        protected virtual object VisitStaticArrayMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<object> args = this.VisitExpressionList(m.Arguments);
                    object arg = args[1];

                    var memberExpr = (MemberExpression)m.Arguments[0];
                    var memberName = memberExpr.Member.Name;
                    var expr = "contains({0}, {1})".Fmt(
                        memberName, GetValueAsParam(arg));
                    return expr;

                default:
                    throw new NotSupportedException();
            }
        }

        private bool IsEnumerableMethod(MethodCallExpression m)
        {
            if (m.Object != null
                && m.Object.Type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                && m.Object.Type != typeof(string)
                && m.Method.Name == "Contains")
            {
                return m.Arguments.Count == 1;
            }

            return false;
        }

        protected virtual object VisitEnumerableMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<object> args = this.VisitExpressionList(m.Arguments);
                    object arg = args[0];
                    var memberName = ((MemberExpression) m.Object).Member.Name;
                    var expr = "contains({0}, {1})".Fmt(
                        memberName, GetValueAsParam(arg));
                    return expr;

                default:
                    throw new NotSupportedException();
            }
        }

        public static List<object> Flatten(IEnumerable list)
        {
            var ret = new List<object>();
            if (list == null) return ret;

            foreach (var item in list)
            {
                if (item == null) continue;

                var arr = item as IEnumerable;
                if (arr != null && !(item is string))
                {
                    ret.AddRange(arr.Cast<object>());
                }
                else
                {
                    ret.Add(item);
                }
            }
            return ret;
        }

        protected virtual object VisitNew(NewExpression nex)
        {
            // TODO : check !
            var member = Expression.Convert(nex, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(member);
            try
            {
                var getter = lambda.Compile();
                return getter();
            }
            catch (InvalidOperationException)
            { // FieldName ?
                var exprs = VisitExpressionList(nex.Arguments);
                var r = new StringBuilder();
                foreach (object e in exprs)
                {
                    if (r.Length > 0)
                        r.Append(",");

                    r.Append(e);
                }
                return r.ToString();
            }
        }

        private bool IsColumnAccess(MethodCallExpression m)
        {
            if (m.Object != null && m.Object as MethodCallExpression != null)
                return IsColumnAccess(m.Object as MethodCallExpression);

            var exp = m.Object as MemberExpression;
            return exp != null
                && exp.Expression != null
                && exp.Expression.NodeType == ExpressionType.Parameter;
        }

        protected virtual object VisitColumnAccessMethod(MethodCallExpression m)
        {
            List<object> args = this.VisitExpressionList(m.Arguments);
            var quotedColName = Visit(m.Object);
            var statement = "";

            var wildcardArg = args.Count > 0 ? EscapeWildcards(args[0].ToString()) : "";
            var escapeSuffix = wildcardArg.IndexOf('^') >= 0 ? " escape '^'" : "";
            switch (m.Method.Name)
            {
                case "Trim":
                    statement = string.Format("ltrim(rtrim({0}))", quotedColName);
                    break;
                case "LTrim":
                    statement = string.Format("ltrim({0})", quotedColName);
                    break;
                case "RTrim":
                    statement = string.Format("rtrim({0})", quotedColName);
                    break;
                case "ToUpper":
                    statement = string.Format("upper({0})", quotedColName);
                    break;
                case "ToLower":
                    statement = string.Format("lower({0})", quotedColName);
                    break;
                case "StartsWith":
                    statement = string.Format("begins_with({0}, {1})",
                        quotedColName, GetValueAsParam(wildcardArg));
                    break;
                case "EndsWith":
                    statement = string.Format("upper({0}) like {1}{2}",
                        quotedColName, GetQuotedValue("%" +
                        wildcardArg.ToUpper()), escapeSuffix);
                    break;
                case "Contains":
                    statement = string.Format("contains({0}, {1})",
                        quotedColName, GetValueAsParam(wildcardArg));
                    break;
                case "Substring":
                    var startIndex = int.Parse(args[0].ToString()) + 1;
                    if (args.Count == 2)
                    {
                        var length = int.Parse(args[1].ToString());
                        statement = GetSubstringSql(quotedColName, startIndex, length);
                    }
                    else
                    {
                        statement = GetSubstringSql(quotedColName, startIndex);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
            return new PartialString(statement);
        }

        public virtual string GetSubstringSql(object quotedColumn, int startIndex, int? length = null)
        {
            return length != null
                ? string.Format("substring({0} from {1} for {2})", quotedColumn, startIndex, length.Value)
                : string.Format("substring({0} from {1})", quotedColumn, startIndex);
        }

        public virtual string EscapeWildcards(string value)
        {
            if (value == null)
                return null;

            return value
                .Replace("^", @"^^")
                .Replace(@"\", @"^\")
                .Replace("_", @"^_")
                .Replace("%", @"^%");
        }

        protected virtual object VisitNewArray(NewArrayExpression na)
        {
            var exprs = VisitExpressionList(na.Expressions);
            var sb = new StringBuilder();
            foreach (var e in exprs)
            {
                sb.Append(sb.Length > 0 ? "," + e : e);
            }
            return sb.ToString();
        }

        protected virtual object VisitMemberInit(MemberInitExpression exp)
        {
            return Expression.Lambda(exp).Compile().DynamicInvoke();
        }

        protected virtual object VisitLambda(LambdaExpression lambda)
        {
            if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression m = lambda.Body as MemberExpression;
                if (m.Expression != null)
                {
                    string r = VisitMemberAccess(m).ToString();
                    return string.Format("{0}={1}", r, GetQuotedTrueValue());
                }

            }
            return Visit(lambda.Body);
        }

        protected virtual object VisitMemberAccess(MemberExpression m)
        {
            if (m.Member.Name == "Length" || m.Member.Name == "Count")
            {
                return new PartialString("size({0})".Fmt(((MemberExpression)m.Expression).Member.Name));
            }

            if (m.Expression != null &&
                 (m.Expression.NodeType == ExpressionType.Parameter ||
                  m.Expression.NodeType == ExpressionType.Convert))
            {
                return GetMemberExpression(m);
            }

            var member = Expression.Convert(m, typeof(object));
            var lambda = Expression.Lambda<Func<object>>(member);
            var getter = lambda.Compile();
            return getter();
        }

        private object GetMemberExpression(MemberExpression m)
        {
            var propertyInfo = m.Member as PropertyInfo;

            var modelType = m.Expression.Type;
            if (m.Expression.NodeType == ExpressionType.Convert)
            {
                var unaryExpr = m.Expression as UnaryExpression;
                if (unaryExpr != null)
                {
                    modelType = unaryExpr.Operand.Type;
                }
            }

            OnVisitMemberType(modelType);

            if (propertyInfo != null && propertyInfo.PropertyType.IsEnum)
                return new EnumMemberAccess(m.Member.Name, propertyInfo.PropertyType);

            return new PartialString(m.Member.Name);
        }

        protected object GetQuotedTrueValue()
        {
            return new PartialString("true");
        }

        protected object GetQuotedFalseValue()
        {
            return new PartialString("false");
        }

        protected virtual object VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
                return new PartialString("null");

            return c.Value;
        }

        protected virtual object VisitBinary(BinaryExpression b)
        {
            object originalLeft = null, originalRight = null, left, right;
            var operand = BindOperant(b.NodeType);   //sep= " " ??
            if (operand == "AND" || operand == "OR")
            {
                var m = b.Left as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    left = new PartialString(string.Format("{0}={1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    left = Visit(b.Left);

                m = b.Right as MemberExpression;
                if (m != null && m.Expression != null
                    && m.Expression.NodeType == ExpressionType.Parameter)
                    right = new PartialString(string.Format("{0}={1}", VisitMemberAccess(m), GetQuotedTrueValue()));
                else
                    right = Visit(b.Right);

                if (left as PartialString == null && right as PartialString == null)
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return result;
                }

                if (left as PartialString == null)
                    left = ((bool)left) ? GetTrueExpression() : GetFalseExpression();
                if (right as PartialString == null)
                    right = ((bool)right) ? GetTrueExpression() : GetFalseExpression();
            }
            else if ((operand == "=" || operand == "<>") && b.Left is MethodCallExpression && ((MethodCallExpression)b.Left).Method.Name == "CompareString")
            {
                //Handle VB.NET converting (x => x.Name == "Foo") into (x => CompareString(x.Name, "Foo", False)
                var methodExpr = (MethodCallExpression)b.Left;
                var args = this.VisitExpressionList(methodExpr.Arguments);
                object quotedColName = args[0];
                object value = GetValue(args[1], typeof(string));
                return new PartialString("({0} {1} {2})".Fmt(quotedColName, operand, value));
            }
            else
            {
                originalLeft = left = Visit(b.Left);
                originalRight = right = Visit(b.Right);

                var leftEnum = left as EnumMemberAccess;
                var rightEnum = right as EnumMemberAccess;

                var rightNeedsCoercing = leftEnum != null && rightEnum == null;
                var leftNeedsCoercing = rightEnum != null && leftEnum == null;

                if (rightNeedsCoercing)
                {
                    var rightPartialSql = right as PartialString;
                    if (rightPartialSql == null)
                    {
                        right = GetValue(right, leftEnum.EnumType);
                    }
                }
                else if (leftNeedsCoercing)
                {
                    var leftPartialSql = left as PartialString;
                    if (leftPartialSql == null)
                    {
                        left = GetQuotedValue(left, rightEnum.EnumType);
                    }
                }
                else if (left as PartialString == null && right as PartialString == null)
                {
                    var result = Expression.Lambda(b).Compile().DynamicInvoke();
                    return result;
                }
                else if (left as PartialString == null)
                {
                    left = GetQuotedValue(left, left != null ? left.GetType() : null);
                }
                else if (right as PartialString == null)
                {
                    right = GetValue(right, right != null ? right.GetType() : null);
                }
            }

            if (operand == "=" && right.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) operand = "is";
            else if (operand == "<>" && right.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)) operand = "is not";

            VisitFilter(operand, originalLeft, originalRight, ref left, ref right);

            switch (operand)
            {
                case "MOD":
                case "COALESCE":
                    return new PartialString(string.Format("{0}({1},{2})", operand, left, right));
                default:
                    return new PartialString("(" + left + sep + operand + sep + right + ")");
            }
        }

        protected bool VisitedExpressionIsTableColumn = false;
        protected bool SkipParameterizationForThisExpression { get; set; }

        protected void VisitFilter(string operand, object originalLeft, object originalRight, ref object left, ref object right)
        {
            if (SkipParameterizationForThisExpression)
                return;

            if (VisitedExpressionIsTableColumn || (originalRight is DateTimeOffset))
                return;

            var leftEnum = originalLeft as EnumMemberAccess;
            var rightEnum = originalRight as EnumMemberAccess;

            if (leftEnum != null && rightEnum != null)
                return;

            if (operand == "AND" || operand == "OR" || operand == "is" || operand == "is not")
                return;

            ConvertToPlaceholderAndParameter(ref right);
        }

        protected void OnVisitMemberType(Type modelType)
        {
            var tableDef = DynamoMetadata.TryGetTable(modelType);
            if (tableDef != null)
                VisitedExpressionIsTableColumn = true;
        }

        protected void ConvertToPlaceholderAndParameter(ref object right)
        {
            var paramName = ":p" + Params.Count;
            var paramValue = right;

            Params[paramName] = paramValue;

            right = paramName;
        }

        protected virtual List<object> VisitNewArrayFromExpressionList(NewArrayExpression na)
        {
            var exprs = VisitExpressionList(na.Expressions);
            return exprs;
        }

        public string GetValueAsParam(object value)
        {
            var paramName = ":p" + Params.Count;
            Params[paramName] = value;
            return paramName;
        }

        public virtual object GetValue(object value, Type type)
        {
            if (SkipParameterizationForThisExpression)
                return GetQuotedValue(value, type);

            return value ?? "null";
        }

        private string GetQuotedValue(object value, Type fieldType)
        {
            return GetQuotedValue(value.ToString());
        }

        public virtual string GetQuotedValue(string paramValue)
        {
            return "'" + paramValue.Replace("'", "''") + "'";
        }

        protected virtual string BindOperant(ExpressionType e)
        {
            switch (e)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "MOD";
                case ExpressionType.Coalesce:
                    return "COALESCE";
                default:
                    return e.ToString();
            }
        }

        protected object GetTrueExpression()
        {
            return new PartialString(string.Format("({0}={1})", GetQuotedTrueValue(), GetQuotedTrueValue()));
        }

        protected object GetFalseExpression()
        {
            return new PartialString(string.Format("({0}={1})", GetQuotedTrueValue(), GetQuotedFalseValue()));
        }

        protected virtual object VisitUnary(UnaryExpression u)
        {
            object o;
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    o = Visit(u.Operand);
                    return new PartialString("not " + o);
                case ExpressionType.Convert:
                    if (u.Method != null)
                        return Expression.Lambda(u).Compile().DynamicInvoke();
                    break;
                case ExpressionType.ArrayLength:
                    o = Visit(u.Operand);
                    return new PartialString("size({0})".Fmt(o));
            }
            return Visit(u.Operand);
        }

        protected bool IsFieldName(object quotedExp)
        {
            var fieldExpr = quotedExp.ToString();
            var unquotedExpr = fieldExpr.StripQuotes();

            var isTableField = DbType.Fields
                .Any(x => x.Name == unquotedExpr);
            if (isTableField)
                return true;

            return false;
        }
    }

    public class PartialString
    {
        public PartialString(string text)
        {
            Text = text;
        }
        public string Text { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }

    public class EnumMemberAccess : PartialString
    {
        public EnumMemberAccess(string text, Type enumType)
            : base(text)
        {
            if (!enumType.IsEnum) throw new ArgumentException("Type not valid", "enumType");

            EnumType = enumType;
        }

        public Type EnumType { get; private set; }
    }

    public static class Dynamo
    {
        public static bool AttributeExists(object property)
        {
            return true;
        }

        public static bool AttributeNotExists(object property)
        {
            return true;
        }

        public static bool AttributeType(object property, string dynamoType)
        {
            return true;
        }

        public static bool BeginsWith(object property, string needle)
        {
            return true;
        }

        public static bool Contains(object property, object needle)
        {
            return true;
        }

        public static long Size(object property)
        {
            return default(long);
        }
    }
}