using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionModule.Data.Exceptions
{
    public class CreateSubscriptionException : Exception
    {
        public CreateSubscriptionException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
