using System;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws
{
    public abstract class AwsConnectionFactory<T>
    {
        private readonly Func<T> _clientFactory;

        protected AwsConnectionFactory(Func<T> clientFactory)
        {
            Guard.AgainstNullArgument(clientFactory, "clientFactory");
            _clientFactory = clientFactory;
        }

        public T GetClient()
        {
            return _clientFactory();
        }

    }
}