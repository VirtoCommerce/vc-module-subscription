using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SubscriptionModule.Test
{
    public class PaymentPlanServiceUnitTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<ISubscriptionRepository> _subscriptionRepositoryFactoryMock;

        public PaymentPlanServiceUnitTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _subscriptionRepositoryFactoryMock = new Mock<ISubscriptionRepository>();
        }

        [Fact]
        public async Task GetByIdsAsync_GetThenSavePaymentPlan_ReturnCachedPaymentPlan()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var newPaymentPlan = new PaymentPlan { Id = id };
            var newPaymentPlanEntity = AbstractTypeFactory<PaymentPlanEntity>.TryCreateInstance().FromModel(newPaymentPlan, new PrimaryKeyResolvingMap());
            var service = GetPaymentPlanServiceWithPlatformMemoryCache();
            _subscriptionRepositoryFactoryMock.Setup(x => x.Add(newPaymentPlanEntity))
                .Callback(() =>
                {
                    _subscriptionRepositoryFactoryMock.Setup(o => o.GetPaymentPlansByIdsAsync(new[] { id }))
                        .ReturnsAsync(new[] { newPaymentPlanEntity });
                });

            //Act
            var nullPaymentPlan = await service.GetByIdsAsync(new []{ id }, null);
            await service.SavePlansAsync(new[] { newPaymentPlan });
            var paymentPlan = await service.GetByIdsAsync(new[] { id }, null);

            //Assert
            Assert.NotEqual(nullPaymentPlan, paymentPlan);
        }

        [Fact]
        public async Task GetByIdAsync_TryToGetPlanWithNullId()
        {
            var planService = GetPaymentPlanServiceWithPlatformMemoryCache();

            // Exception should not be thrown
            await planService.GetByIdsAsync(new string[] {null});
        }


        private PaymentPlanService GetPaymentPlanServiceWithPlatformMemoryCache()
        {
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);
            _subscriptionRepositoryFactoryMock.Setup(ss => ss.UnitOfWork).Returns(_unitOfWorkMock.Object);

            return GetPaymentPlanService(platformMemoryCache);
        }

        private PaymentPlanService GetPaymentPlanService(IPlatformMemoryCache platformMemoryCache)
        {
            return new PaymentPlanService(
                () => _subscriptionRepositoryFactoryMock.Object,
                _eventPublisherMock.Object,
                platformMemoryCache
                );
        }
    }
}
