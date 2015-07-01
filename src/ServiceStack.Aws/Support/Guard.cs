using System;

namespace ServiceStack.Aws.Support
{
    public static class Guard
    {
        public static void Against(Boolean assert, String message)
        {
            if (!assert)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        public static void Against<TException>(Boolean assert, String message) where TException : Exception
        {
            if (!assert)
            {
                return;
            }

            throw (TException)Activator.CreateInstance(typeof(TException), message);
        }

        public static void AgainstNullArgument<T>(T source, String argumentName)
            where T : class 
        {
            if (source != null)
            {
                return;
            }

            throw new ArgumentNullException(argumentName);
        }

        public static void AgainstArgumentOutOfRange(Boolean assert, String argumentName)
        {
            if (!assert)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(argumentName);
        }
        
    }
}