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
        }
        public ICollection<Subscription> Subscriptions { get; set; }
    }


    public sealed class SubscriptionExportImport
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionSearchService _subscriptionSearchService;

        public SubscriptionExportImport(ISubscriptionService subscriptionService, ISubscriptionSearchService subscriptionSearchService)
        {
            _subscriptionService = subscriptionService;
            _subscriptionSearchService = subscriptionSearchService;
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
            return retVal;
        }
    }

}