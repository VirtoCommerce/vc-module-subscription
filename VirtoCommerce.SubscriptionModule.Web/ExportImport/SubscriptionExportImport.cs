using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Web.ExportImport
{
    public sealed class BackupObject
    {
        public BackupObject()
        {
            Subscriptions = new List<Subscription>();
            PaymentPlans = new List<PaymentPlan>();
        }
        public ICollection<PaymentPlan> PaymentPlans { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
    }


    public sealed class SubscriptionExportImport
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly IPaymentPlanSearchService _paymentPlanSearchService;
        private readonly IPaymentPlanService _paymentPlanService;

        public SubscriptionExportImport(ISubscriptionService subscriptionService, ISubscriptionSearchService subscriptionSearchService, IPaymentPlanSearchService planSearchService, IPaymentPlanService paymentPlanService)
        {
            _subscriptionService = subscriptionService;
            _subscriptionSearchService = subscriptionSearchService;
            _paymentPlanSearchService = planSearchService;
            _paymentPlanService = paymentPlanService;
        }


        public void DoExport(Stream backupStream, Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = GetBackupObject(progressCallback);
            backupObject.SerializeJson(backupStream);
        }

        public void DoImport(Stream backupStream, Action<ExportImportProgressInfo> progressCallback)
        {
            var backupObject = backupStream.DeserializeJson<BackupObject>();

            var progressInfo = new ExportImportProgressInfo();
            var totalCount = backupObject.Subscriptions.Count();
            var take = 20;
            for (int skip = 0; skip < totalCount; skip += take)
            {
                _subscriptionService.SaveSubscriptions(backupObject.Subscriptions.Skip(skip).Take(take).ToArray());
                progressInfo.Description = String.Format("{0} of {1} subscriptions imported", Math.Min(skip + take, totalCount), totalCount);
                progressCallback(progressInfo);
            }

            totalCount = backupObject.PaymentPlans.Count();
            for (int skip = 0; skip < totalCount; skip += take)
            {
                _paymentPlanService.SavePlans(backupObject.PaymentPlans.Skip(skip).Take(take).ToArray());
                progressInfo.Description = String.Format("{0} of {1} payment plans imported", Math.Min(skip + take, totalCount), totalCount);
                progressCallback(progressInfo);
            }
        }

        private BackupObject GetBackupObject(Action<ExportImportProgressInfo> progressCallback)
        {

            var retVal = new BackupObject();
            var progressInfo = new ExportImportProgressInfo();

            var take = 20;

            var searchResponse = _subscriptionSearchService.SearchSubscriptions(new SubscriptionSearchCriteria { Take = 0, ResponseGroup = SubscriptionResponseGroup.Default.ToString() });

            for (int skip = 0; skip < searchResponse.TotalCount; skip += take)
            {
                searchResponse = _subscriptionSearchService.SearchSubscriptions(new SubscriptionSearchCriteria { Skip = skip, Take = take, ResponseGroup = SubscriptionResponseGroup.Default.ToString() });

                progressInfo.Description = String.Format("{0} of {1} subscriptions loading", Math.Min(skip + take, searchResponse.TotalCount), searchResponse.TotalCount);
                progressCallback(progressInfo);
                retVal.Subscriptions.AddRange(searchResponse.Results);
            }

            var paymentPlanSearchResponse = _paymentPlanSearchService.SearchPlans(new PaymentPlanSearchCriteria { Take = 0 });

            for (int skip = 0; skip < paymentPlanSearchResponse.TotalCount; skip += take)
            {
                paymentPlanSearchResponse = _paymentPlanSearchService.SearchPlans(new PaymentPlanSearchCriteria { Skip = skip, Take = take });

                progressInfo.Description = String.Format("{0} of {1} payment plans loading", Math.Min(skip + take, paymentPlanSearchResponse.TotalCount), paymentPlanSearchResponse.TotalCount);
                progressCallback(progressInfo);
                retVal.PaymentPlans.AddRange(paymentPlanSearchResponse.Results);
            }
            return retVal;
        }
    }

}