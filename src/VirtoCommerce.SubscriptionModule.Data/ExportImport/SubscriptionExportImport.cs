using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Data.ExportImport
{
    public sealed class SubscriptionExportImport(
        ISubscriptionService subscriptionService,
        ISubscriptionSearchService subscriptionSearchService,
        IPaymentPlanSearchService paymentPlanSearchService,
        IPaymentPlanService paymentPlanService,
        JsonSerializer jsonSerializer)
    {
        private const int BatchSize = 20;

        private readonly Dictionary<Type, string> _typeDescriptions = new()
        {
            { typeof(PaymentPlan), "payment plans" }, { typeof(Subscription), "subscriptions" },
        };


        public async Task DoExportAsync(Stream backupStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progressInfo = new ExportImportProgressInfo("Starting data export");
            progressCallback(progressInfo);

            await using var streamWriter = new StreamWriter(backupStream, Encoding.UTF8);
            await using var jsonTextWriter = new JsonTextWriter(streamWriter);
            await jsonTextWriter.WriteStartObjectAsync();

            await jsonTextWriter.WritePropertyNameAsync("PaymentPlans");
            await jsonTextWriter.WriteStartArrayAsync();
            var processedCount = 0;

            var paymentPlanSearchCriteria = AbstractTypeFactory<PaymentPlanSearchCriteria>.TryCreateInstance();
            paymentPlanSearchCriteria.Take = BatchSize;

            await foreach (var paymentPlanSearchResponse in paymentPlanSearchService.SearchBatchesAsync(paymentPlanSearchCriteria))
            {
                cancellationToken.ThrowIfCancellationRequested();

                processedCount += paymentPlanSearchResponse.Results.Count;
                progressInfo.Description = $"{Math.Min(processedCount, paymentPlanSearchResponse.TotalCount)} of {paymentPlanSearchResponse.TotalCount} payment plans loading";
                progressCallback(progressInfo);

                foreach (var paymentPlan in paymentPlanSearchResponse.Results)
                {
                    jsonSerializer.Serialize(jsonTextWriter, paymentPlan);
                }
            }

            await jsonTextWriter.WriteEndArrayAsync();

            await jsonTextWriter.WritePropertyNameAsync("Subscriptions");
            await jsonTextWriter.WriteStartArrayAsync();
            processedCount = 0;

            var subscriptionSearchCriteria = AbstractTypeFactory<SubscriptionSearchCriteria>.TryCreateInstance();
            subscriptionSearchCriteria.Take = BatchSize;
            subscriptionSearchCriteria.ResponseGroup = nameof(SubscriptionResponseGroup.Default);

            await foreach (var subscriptionSearchResult in subscriptionSearchService.SearchBatchesAsync(subscriptionSearchCriteria))
            {
                cancellationToken.ThrowIfCancellationRequested();

                processedCount += subscriptionSearchResult.Results.Count;
                progressInfo.Description = $"{Math.Min(processedCount, subscriptionSearchResult.TotalCount)} of {subscriptionSearchResult.TotalCount} subscriptions loading";
                progressCallback(progressInfo);

                foreach (var subscription in subscriptionSearchResult.Results)
                {
                    jsonSerializer.Serialize(jsonTextWriter, subscription);
                }
            }

            await jsonTextWriter.WriteEndArrayAsync();

            await jsonTextWriter.WriteEndObjectAsync();
            await jsonTextWriter.FlushAsync();
        }

        public async Task DoImportAsync(Stream backupStream, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progressInfo = new ExportImportProgressInfo("Preparing for import");
            progressCallback(progressInfo);

            using var streamReader = new StreamReader(backupStream, Encoding.UTF8);
            await using var jsonReader = new JsonTextReader(streamReader);
            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }

                switch (jsonReader.Value?.ToString())
                {
                    case "PaymentPlans":
                        await InternalImportAsync<PaymentPlan>(jsonReader, progressInfo);
                        break;
                    case "Subscriptions":
                        await InternalImportAsync<Subscription>(jsonReader, progressInfo);
                        break;
                }
            }
        }

        private async Task InternalImportAsync<T>(JsonReader reader, ExportImportProgressInfo progressInfo) where T : AuditableEntity
        {
            if (!TryReadCollectionOf<T>(reader, out var importedCollection))
            {
                return;
            }

            var type = typeof(T);

            var totalCount = importedCollection.Count;

            for (var skip = 0; skip < totalCount; skip += BatchSize)
            {
                var currentItems = importedCollection.Skip(skip).Take(BatchSize).ToArray();

                if (type == typeof(PaymentPlan))
                {
                    await paymentPlanService.SaveChangesAsync(currentItems as PaymentPlan[]);
                }
                else if (type == typeof(Subscription))
                {
                    await subscriptionService.SaveChangesAsync(currentItems as Subscription[]);
                }

                progressInfo.Description = $"{Math.Min(skip + BatchSize, totalCount)} of {totalCount} {_typeDescriptions[type]} have been imported.";
            }
        }

        private bool TryReadCollectionOf<TValue>(JsonReader jsonReader, out IReadOnlyCollection<TValue> values)
        {
            jsonReader.Read();
            if (jsonReader.TokenType == JsonToken.StartArray)
            {
                jsonReader.Read();

                var items = new List<TValue>();
                while (jsonReader.TokenType != JsonToken.EndArray)
                {
                    var item = jsonSerializer.Deserialize<TValue>(jsonReader);
                    items.Add(item);

                    jsonReader.Read();
                }

                values = items;
                return true;
            }

            values = Array.Empty<TValue>();
            return false;
        }
    }
}
