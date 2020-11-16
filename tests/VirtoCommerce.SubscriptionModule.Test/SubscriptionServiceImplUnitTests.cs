using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SubscriptionModule.Test
{
    public class SubscriptionServiceImplUnitTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IStoreService> _storeServiceMock;
        private readonly Mock<ICustomerOrderService> _customerOrderServiceMock;
        private readonly Mock<ICustomerOrderSearchService> _customerOrderSearchServiceMock;
        private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryFactoryMock;
        private readonly Mock<IUniqueNumberGenerator> _uniqueNumberGeneratorMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;

        public SubscriptionServiceImplUnitTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _storeServiceMock = new Mock<IStoreService>();
            _customerOrderServiceMock = new Mock<ICustomerOrderService>();
            _customerOrderSearchServiceMock = new Mock<ICustomerOrderSearchService>();
            _subscriptionRepositoryFactoryMock = new Mock<ISubscriptionRepository>();
            _uniqueNumberGeneratorMock = new Mock<IUniqueNumberGenerator>();
            _eventPublisherMock = new Mock<IEventPublisher>();
        }

        [Fact]
        public async Task GetByIdsAsync_GetThenSaveSubscription_ReturnCachedSubscription()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var newSubscription = new Subscription { Id = id };
            var newSubscriptionEntity = AbstractTypeFactory<SubscriptionEntity>.TryCreateInstance().FromModel(newSubscription, new PrimaryKeyResolvingMap());
            var service = GetSubscriptionServiceWithPlatformMemoryCache();
            _subscriptionRepositoryFactoryMock.Setup(x => x.Add(newSubscriptionEntity))
                .Callback(() =>
                {
                    _subscriptionRepositoryFactoryMock.Setup(o => o.GetSubscriptionsByIdsAsync(new[] { id }, null))
                        .ReturnsAsync(new[] { newSubscriptionEntity });
                });

            //Act
            var nullSubscription = await service.GetByIdsAsync(new[] { id }, null);
            await service.SaveSubscriptionsAsync(new[] { newSubscription });
            var subscription = await service.GetByIdsAsync(new[] { id }, null);

            //Assert
            Assert.NotEqual(nullSubscription, subscription);
        }


        private SubscriptionServiceImpl GetSubscriptionServiceWithPlatformMemoryCache()
        {
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);
            
            return GetSubscriptionServiceImpl(platformMemoryCache);
        }

        private SubscriptionServiceImpl GetSubscriptionServiceImpl(IPlatformMemoryCache platformMemoryCache)
        {
            _subscriptionRepositoryFactoryMock.Setup(ss => ss.UnitOfWork).Returns(_unitOfWorkMock.Object);
            _customerOrderSearchServiceMock
                .Setup(x => x.SearchCustomerOrdersAsync(It.IsAny<CustomerOrderSearchCriteria>()))
                .ReturnsAsync(new CustomerOrderSearchResult());
            _storeServiceMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Store {Settings = new List<ObjectSettingEntry>()});

            return new SubscriptionServiceImpl(
                _storeServiceMock.Object,
                _customerOrderServiceMock.Object,
                _customerOrderSearchServiceMock.Object,
                () => _subscriptionRepositoryFactoryMock.Object,
                _uniqueNumberGeneratorMock.Object,
                _eventPublisherMock.Object,
                platformMemoryCache,
                null
                );
        }
    }
}
