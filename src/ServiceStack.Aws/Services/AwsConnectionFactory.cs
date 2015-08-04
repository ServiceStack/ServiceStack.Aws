using System;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Services
{
    public abstract class AwsConnectionFactory<T>
    {
        private readonly Func<T> clientFactory;

        protected AwsConnectionFactory(Func<T> clientFactory)
        {
            Guard.AgainstNullArgument(clientFactory, "clientFactory");
            this.clientFactory = clientFactory;
        }

        public T GetClient()
        {
            return clientFactory();
        }

    }
}