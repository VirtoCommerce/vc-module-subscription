using System;

namespace VirtoCommerce.SubscriptionModule.Core;

public class SubscriptionException : Exception
{
    public SubscriptionException()
    {
    }

    public SubscriptionException(string message) : base(message)
    {
    }

    public SubscriptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
