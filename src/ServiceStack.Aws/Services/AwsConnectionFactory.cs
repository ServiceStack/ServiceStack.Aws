using System;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Services
{
    public abstract class AwsConnectionFactory<T>
    {
        private readonly Func<T> clientFactory;

        protected AwsConnectionFactory(Func<T> cliFactory)
        {
            Guard.AgainstNullArgument(clientFactory, "clientFactory");
            clientFactory = cliFactory;
        }

        public T GetClient()
        {
            return clientFactory();
        }

    }
}