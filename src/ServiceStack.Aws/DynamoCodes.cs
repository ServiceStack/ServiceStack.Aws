namespace ServiceStack.Aws
{
    public static class DynamoType
    {
        public const string String = "S";
        public const string Number = "N";
        public const string Binary = "B";
        public const string Bool = "BOOL";

        public const string StringSet = "SS";
        public const string NumberSet = "NS";
        public const string BinarySet = "BS";

        public const string List = "L";
        public const string Map = "M";
        public const string Null = "Null";
    }

    public static class DynamoAction
    {
        public const string Add = "ADD";
    }

    public static class DynamoReturn
    {
        public const string AllNew = "ALL_NEW";
    }

    public static class DynamoStatus
    {
        public const string Active = "ACTIVE";
    }

    //KeyType
    public static class DynamoKey
    {
        public const string Hash = "HASH";
        public const string Range = "RANGE";
    }
}