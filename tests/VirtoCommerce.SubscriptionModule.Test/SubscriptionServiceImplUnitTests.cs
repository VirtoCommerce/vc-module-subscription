using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SubscriptionModule.Tests
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
                    _subscriptionRepositoryFactoryMock.Setup(o => o.GetSubscriptionsByIdsAsync(new[] { id }, It.IsAny<string>()))
                        .ReturnsAsync([newSubscriptionEntity]);
                });

            //Act
            var nullSubscription = await service.GetByIdAsync(id);
            await service.SaveChangesAsync([newSubscription]);
            var subscription = await service.GetByIdAsync(id);

            //Assert
            Assert.NotEqual(nullSubscription, subscription);
        }

        [Fact]
        public async Task SaveChangesAsync_NewSubscription_ChangedEventCarriesOrderPrototype()
        {
            //Arrange
            var prototype = new CustomerOrder { Id = "prototype", LanguageCode = "en-US" };
            var newSubscription = new Subscription { Id = Guid.NewGuid().ToString(), CustomerOrderPrototype = prototype };
            GenericChangedEntryEvent<Subscription> changedEvent = null;
            _eventPublisherMock
                .Setup(x => x.Publish(It.Is<GenericChangedEntryEvent<Subscription>>(e => e is SubscriptionChangedEvent), It.IsAny<CancellationToken>()))
                .Callback<GenericChangedEntryEvent<Subscription>, CancellationToken>((message, _) => changedEvent = message)
                .Returns(Task.CompletedTask);
            var service = GetSubscriptionServiceWithPlatformMemoryCache();

            //Act
            await service.SaveChangesAsync([newSubscription]);

            //Assert
            var newEntry = changedEvent.ChangedEntries.Single().NewEntry;
            newEntry.CustomerOrderPrototype.Should().BeSameAs(prototype);
            newEntry.CustomerOrderPrototypeId.Should().Be("prototype");
        }


        private SubscriptionService GetSubscriptionServiceWithPlatformMemoryCache()
        {
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);

            return GetSubscriptionServiceImpl(platformMemoryCache);
        }

        private SubscriptionService GetSubscriptionServiceImpl(IPlatformMemoryCache platformMemoryCache)
        {
            _subscriptionRepositoryFactoryMock.Setup(ss => ss.UnitOfWork).Returns(_unitOfWorkMock.Object);
            _customerOrderSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<CustomerOrderSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new CustomerOrderSearchResult());
            _storeServiceMock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new Store { Settings = [] }]);

            return new SubscriptionService(
                () => _subscriptionRepositoryFactoryMock.Object,
                _eventPublisherMock.Object,
                platformMemoryCache,
                _customerOrderServiceMock.Object,
                _customerOrderSearchServiceMock.Object,
                null
                );
        }
    }
}
