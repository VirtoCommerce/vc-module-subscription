using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using VirtoCommerce.OrdersModule.Core.Events;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.BackgroundJobs;
using VirtoCommerce.SubscriptionModule.Data.Handlers;
using Xunit;

namespace VirtoCommerce.SubscriptionModule.Tests
{
    public class CreateSubscriptionsFromOrdersJobTests
    {
        // Mirrors VirtoCommerce.BackgroundJobs.JobJsonSettings.Default, which the BackgroundJobs module uses to store
        // payloads: no TypeNameHandling and none of the platform's polymorphic converters.
        private static readonly JsonSerializerSettings _jobJsonSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
        };

        private readonly Mock<ICustomerOrderService> _customerOrderServiceMock = new();
        private readonly Mock<CreateSubscriptionOrderChangedEventHandler> _eventHandlerMock;

        public CreateSubscriptionsFromOrdersJobTests()
        {
            _eventHandlerMock = new Mock<CreateSubscriptionOrderChangedEventHandler>(
                Mock.Of<ISubscriptionBuilder>(), Mock.Of<ISubscriptionService>(), _customerOrderServiceMock.Object)
            {
                CallBase = true,
            };
        }

        [Fact]
        public async Task Handle_OrderWithPayment_EnqueuesPayloadThatSurvivesJobSerialization()
        {
            // Arrange
            var addedOrder = new CustomerOrder
            {
                Id = "added-order",
                InPayments = [new PaymentIn { Id = "payment", PaymentMethod = new TestPaymentMethod() }],
            };
            var modifiedOrder = new CustomerOrder { Id = "modified-order" };
            var orderChangedEvent = new OrderChangedEvent(
            [
                new GenericChangedEntry<CustomerOrder>(addedOrder, EntryState.Added),
                new GenericChangedEntry<CustomerOrder>(modifiedOrder, modifiedOrder, EntryState.Modified),
            ]);

            object enqueuedPayload = null;
            var backgroundJobMock = new Mock<IBackgroundJob>();
            backgroundJobMock
                .Setup(x => x.Enqueue<CreateSubscriptionsFromOrdersJob>(It.IsAny<object>(), It.IsAny<EnqueueOptions>(), It.IsAny<CancellationToken>()))
                .Callback<object, EnqueueOptions, CancellationToken>((payload, _, _) => enqueuedPayload = payload)
                .ReturnsAsync("job-id");
            BackgroundJob.Initialize(new ServiceCollection().AddScoped(_ => backgroundJobMock.Object).BuildServiceProvider());

            // Act
            await _eventHandlerMock.Object.Handle(orderChangedEvent);

            // Assert
            enqueuedPayload.Should().BeOfType<CreateSubscriptionsFromOrdersJobPayload>();
            var json = JsonConvert.SerializeObject(enqueuedPayload, _jobJsonSettings);
            var deserialized = JsonConvert.DeserializeObject<CreateSubscriptionsFromOrdersJobPayload>(json, _jobJsonSettings);
            deserialized.OrderIds.Should().Equal("added-order");
        }

        [Fact]
        public async Task Execute_LoadsOrdersByIdAndHandlesThem()
        {
            // Arrange
            var order = new CustomerOrder { Id = "order" };
            _customerOrderServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.SequenceEqual(new[] { "order" })), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([order]);
            _eventHandlerMock.Setup(x => x.HandleOrderChangesInBackground(It.IsAny<CustomerOrder[]>()));
            var job = new CreateSubscriptionsFromOrdersJob(_eventHandlerMock.Object, _customerOrderServiceMock.Object);

            // Act
            await job.Execute(new CreateSubscriptionsFromOrdersJobPayload { OrderIds = ["order"] }, Mock.Of<IJobExecutionContext>(), TestContext.Current.CancellationToken);

            // Assert
            _eventHandlerMock.Verify(x => x.HandleOrderChangesInBackground(It.Is<CustomerOrder[]>(orders => orders.Single() == order)), Times.Once);
        }

        [Fact]
        public async Task Execute_NoOrderIds_DoesNotLoadOrders()
        {
            // Arrange
            var job = new CreateSubscriptionsFromOrdersJob(_eventHandlerMock.Object, _customerOrderServiceMock.Object);

            // Act
            await job.Execute(new CreateSubscriptionsFromOrdersJobPayload(), Mock.Of<IJobExecutionContext>(), TestContext.Current.CancellationToken);

            // Assert
            _customerOrderServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Payload_QueuedByVersions3_1004To3_1006_IsReadAsOrderIds()
        {
            // Arrange: versions 3.1004.0-3.1006.0 stored whole orders, including the abstract PaymentMethod
            const string json = """
                {"Orders":[{"Id":"order-1","InPayments":[{"Id":"payment","PaymentMethod":{"Code":"DefaultManualPaymentMethod"}}]},{"Id":"order-2"}]}
                """;

            // Act
            var payload = JsonConvert.DeserializeObject<CreateSubscriptionsFromOrdersJobPayload>(json, _jobJsonSettings);

            // Assert
            payload.OrderIds.Should().Equal("order-1", "order-2");
        }

        private sealed class TestPaymentMethod() : PaymentMethod(nameof(TestPaymentMethod))
        {
            public override PaymentMethodType PaymentMethodType => default;

            public override PaymentMethodGroupType PaymentMethodGroupType => default;
        }
    }
}
