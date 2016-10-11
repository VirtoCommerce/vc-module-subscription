using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;

namespace SubscriptionModule.Data.Model
{
    public class SubscriptionOrderRelationEntity : Entity
    {
        [StringLength(64)]
        public string CustomerOrderId { get; set; }
        public string SubscriptionId { get; set; }
        public SubscriptionEntity Subscription { get; set; }
    }
}
