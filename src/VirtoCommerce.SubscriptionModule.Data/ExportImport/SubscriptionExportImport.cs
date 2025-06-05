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
    public sealed class SubscriptionExportImport
    {
        private const int BatchSize = 20;

        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly IPaymentPlanSearchService _paymentPlanSearchService;
        private readonly IPaymentPlanService _paymentPlanService;
        private readonly JsonSerializer _jsonSerializer;

        private readonly Dictionary<Type, string> _typeDescriptions = new()
        {
            { typeof(PaymentPlan), "payment plans" }, { typeof(Subscription), "subscriptions" }
        };

        public SubscriptionExportImport(ISubscriptionService subscriptionService, ISubscriptionSearchService subscriptionSearchService,
            IPaymentPlanSearchService planSearchService, IPaymentPlanService paymentPlanService, JsonSerializer jsonSerializer)
        {
            _subscriptionService = subscriptionService;
            _subscriptionSearchService = subscriptionSearchService;
            _paymentPlanSearchService = planSearchService;
            _paymentPlanService = paymentPlanService;

            _jsonSerializer = jsonSerializer;
        }


        public async Task DoExportAsync(Stream backupStream, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progressInfo = new ExportImportProgressInfo("Starting data export");
            progressCallback(progressInfo);

            await using var streamWriter = new StreamWriter(backupStream, Encoding.UTF8);
            using var jsonTextWriter = new JsonTextWriter(streamWriter);
            await jsonTextWriter.WriteStartObjectAsync();

            var paymentPlanSearchResponse = await _paymentPlanSearchService.SearchAsync(new PaymentPlanSearchCriteria { Take = 0 });

            await jsonTextWriter.WritePropertyNameAsync("PaymentPlans");
            await jsonTextWriter.WriteStartArrayAsync();
            for (var skip = 0; skip < paymentPlanSearchResponse.TotalCount; skip += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                paymentPlanSearchResponse = await _paymentPlanSearchService.SearchAsync(new PaymentPlanSearchCriteria
                {
                    Skip = skip,
                    Take = BatchSize
                });

                progressInfo.Description = $"{Math.Min(skip + BatchSize, paymentPlanSearchResponse.TotalCount)} of {paymentPlanSearchResponse.TotalCount} payment plans loading";
                progressCallback(progressInfo);

                foreach (var paymentPlan in paymentPlanSearchResponse.Results)
                {
                    _jsonSerializer.Serialize(jsonTextWriter, paymentPlan);
                }
            }
            await jsonTextWriter.WriteEndArrayAsync();

            var searchResponse = await _subscriptionSearchService.SearchAsync(new SubscriptionSearchCriteria
            {
                Take = 0,
                ResponseGroup = SubscriptionResponseGroup.Default.ToString()
            });

            await jsonTextWriter.WritePropertyNameAsync("Subscriptions");
            await jsonTextWriter.WriteStartArrayAsync();
            for (var skip = 0; skip < searchResponse.TotalCount; skip += BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                searchResponse = await _subscriptionSearchService.SearchAsync(new SubscriptionSearchCriteria
                {
                    Skip = skip,
                    Take = BatchSize,
                    ResponseGroup = SubscriptionResponseGroup.Default.ToString()
                });

                progressInfo.Description = $"{Math.Min(skip + BatchSize, searchResponse.TotalCount)} of {searchResponse.TotalCount} subscriptions loading";
                progressCallback(progressInfo);

                foreach (var subscription in searchResponse.Results)
                {
                    _jsonSerializer.Serialize(jsonTextWriter, subscription);
                }
            }
            await jsonTextWriter.WriteEndArrayAsync();

            await jsonTextWriter.WriteEndObjectAsync();
            await jsonTextWriter.FlushAsync();
        }

        public async Task DoImportAsync(Stream backupStream, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var progressInfo = new ExportImportProgressInfo("Preparing for import");
            progressCallback(progressInfo);

            using var streamReader = new StreamReader(backupStream, Encoding.UTF8);
            using var jsonReader = new JsonTextReader(streamReader);
            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType != JsonToken.PropertyName)
                    continue;

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
                    await _paymentPlanService.SaveChangesAsync(currentItems as PaymentPlan[]);
                }
                else if (type == typeof(Subscription))
                {
                    await _subscriptionService.SaveChangesAsync(currentItems as Subscription[]);
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
                    var item = _jsonSerializer.Deserialize<TValue>(jsonReader);
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
