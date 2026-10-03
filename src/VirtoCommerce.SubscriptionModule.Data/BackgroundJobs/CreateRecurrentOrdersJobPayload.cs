namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Payload of <see cref="CreateRecurrentOrdersJobHandler"/>. Carries no data: each occurrence scans every active subscription.
    /// </summary>
    public class CreateRecurrentOrdersJobPayload
    {
    }
}
