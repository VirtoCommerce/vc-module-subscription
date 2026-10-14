using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Handlers;
using VirtoCommerce.SubscriptionModule.Data.Notifications;
using Xunit;

namespace VirtoCommerce.SubscriptionModule.Tests
{
    public class SendNotificationsSubscriptionChangedEventHandlerTests
    {
        private const string CustomerEmail = "customer@example.com";

        private readonly TestHandler _handler;

        public SendNotificationsSubscriptionChangedEventHandlerTests()
        {
            var storeServiceMock = new Mock<IStoreService>();
            storeServiceMock
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new Store { Id = "store", Email = "store@example.com" }]);

            _handler = new TestHandler(storeServiceMock.Object);
        }

        [Fact]
        public async Task SetNotificationParametersAsync_WithOrderPrototype_UsesPrototypeEmailAndLanguage()
        {
            // Arrange
            var notification = new NewSubscriptionEmailNotification();
            var subscription = new Subscription
            {
                Id = "subscription",
                StoreId = "store",
                CustomerOrderPrototype = new CustomerOrder
                {
                    LanguageCode = "de-DE",
                    Addresses = [new Address { Email = "order@example.com" }],
                },
            };

            // Act
            await _handler.SetParametersAsync(notification, subscription);

            // Assert
            notification.To.Should().Be("order@example.com");
            notification.LanguageCode.Should().Be("de-DE");
        }

        [Fact]
        public async Task SetNotificationParametersAsync_WithoutOrderPrototype_FallsBackToCustomerEmail()
        {
            // Arrange
            var notification = new NewSubscriptionEmailNotification();
            var subscription = new Subscription { Id = "subscription", StoreId = "store", CustomerId = "customer" };

            // Act
            await _handler.SetParametersAsync(notification, subscription);

            // Assert
            notification.To.Should().Be(CustomerEmail);
            notification.LanguageCode.Should().BeNull();
        }

        private sealed class TestHandler(IStoreService storeService)
            : SendNotificationsSubscriptionChangedEventHandler(
                Mock.Of<INotificationSearchService>(),
                Mock.Of<INotificationSender>(),
                storeService,
                () => null,
                Mock.Of<IMemberService>())
        {
            public Task SetParametersAsync(SubscriptionEmailNotificationBase notification, Subscription subscription)
            {
                return SetNotificationParametersAsync(notification, subscription);
            }

            protected override Task<string> GetCustomerEmailAsync(string customerId)
            {
                return Task.FromResult(CustomerEmail);
            }
        }
    }
}
