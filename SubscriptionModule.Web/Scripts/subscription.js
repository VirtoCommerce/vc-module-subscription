//Call this to register our module to main application
var moduleName = "virtoCommerce.subscriptionModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, ['virtoCommerce.orderModule'])
.config(
  ['$stateProvider', function ($stateProvider) {
      $stateProvider
          .state('workspace.subscriptionModule', {
              url: '/subscriptions',
              templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
              controller: [
                  '$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
                      var blade = {
                          id: 'subscription-list',
                          title: 'subscription.blades.subscription-list.title',
                          controller: 'virtoCommerce.subscriptionModule.subscriptionListController',
                          template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/blades/subscription-list.tpl.html',
                          isClosingDisabled: true
                      };
                      bladeNavigationService.showBlade(blade);
                      $scope.moduleName = 'vc-subscription';
                  }
              ]
          });
  }]
)
.run(
  ['$http', '$compile', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state', 'platformWebApp.bladeNavigationService', 'virtoCommerce.subscriptionModule.subscriptionAPI', 'virtoCommerce.orderModule.knownOperations',
	function ($http, $compile, mainMenuService, widgetService, $state, bladeNavigationService, subscriptionAPI, knownOperations) {

	    //Register module in main menu
	    var menuItem = {
	        path: 'browse/subscriptions',
	        icon: 'fa fa-retweet',
	        title: 'subscription.main-menu-title',
	        priority: 100,
	        action: function () { $state.go('workspace.subscriptionModule'); },
	        permission: 'subscription:access'
	    };
	    mainMenuService.addMenuItem(menuItem);

	    // register Subscription, BalanceAdjustment types as known operations
	    var orderOperation = knownOperations.getOperation('CustomerOrder');
	    var subscriptionKnownChildrenOperations = angular.copy(orderOperation.detailBlade.knownChildrenOperations);
	    subscriptionKnownChildrenOperations.push('BalanceAdjustment');

	    var orderFieldsToExclude = ['isApproved', 'number', 'status', 'discountAmount'];
	    var filteredOrderFields = _.filter(orderOperation.detailBlade.metaFields, function (x) { return !_.contains(orderFieldsToExclude, x.name); });
	    var subscriptionMetaFields = [
            {
                name: 'currentPeriodEnd',
                isReadonly: true,
                title: "subscription.blades.subscription-detail.labels.billing-date",
                valueType: "DateTime"
            },
            {
                name: 'trialEnd',
                title: "subscription.blades.subscription-detail.labels.trial-expiration",
                valueType: "DateTime"
            },
            {
                name: 'endDate',
                isReadonly: true,
                title: "subscription.blades.subscription-detail.labels.expiration",
                valueType: "DateTime"
            }
	    ];

	    _.each(filteredOrderFields, function (x) {
	        subscriptionMetaFields.push(x);
	    });

	    subscriptionMetaFields.push({
	        name: 'couponCode',
	        title: "subscription.blades.subscription-detail.labels.coupon-code",
	        valueType: "ShortText"
	    });
	    subscriptionMetaFields.push({
	        name: 'isProrate',
	        title: "subscription.blades.subscription-detail.labels.prorate",
	        valueType: "Boolean"
	    });

	    // insert number at EXACT location
	    subscriptionMetaFields.splice(4, 0, {
	        name: 'number',
	        isRequired: true,
	        title: "subscription.blades.subscription-detail.labels.number",
	        valueType: "ShortText"
	    });

	    knownOperations.registerOperation({
	        type: 'Subscription',
	        detailBlade: {
	            id: 'subscriptionDetail',
	            controller: 'virtoCommerce.subscriptionModule.subscriptionDetailController',
	            template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/blades/subscription-detail.tpl.html',
	            knownChildrenOperations: subscriptionKnownChildrenOperations,
	            metaFields: subscriptionMetaFields
	        }
	    });

	    $http.get('Modules/$(VirtoCommerce.Orders)/Scripts/blades/customerOrder-detail.tpl.html').then(function (response) {
	        // compile the response, which will put stuff into the cache
	        $compile(response.data);
	    });


	    // BalanceAdjustment
	    knownOperations.registerOperation({
	        type: 'BalanceAdjustment',
	        description: 'subscription.blades.newOperation-wizard.menu.balance-adjustment-operation.description',
	        detailBlade: {
	            template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/shipment-detail.tpl.html'
	        }
	    });


	    // register WIDGETS
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.subscriptionModule.subscriptionOrdersWidgetController',
	        template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/widgets/subscription-orders-widget.tpl.html'
	    }, 'subscriptionDetail');

	    widgetService.registerWidget({
	        controller: 'virtoCommerce.subscriptionModule.orderDynamicPropertyWidgetController',
	        template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/widgets/orderDynamicPropertyWidget.tpl.html'
	    }, 'subscriptionDetail');

	    _.each(widgetService.widgetsMap['customerOrderDetailWidgets'], function (x) {
	        if (x.controller !== 'platformWebApp.dynamicPropertyWidgetController')
	            widgetService.registerWidget(x, 'subscriptionDetail');
	    });

	    var scheduleWidget = {
	        controller: 'virtoCommerce.subscriptionModule.scheduleWidgetController',
	        // isVisible: function (blade) { return blade.isSubscriptionsEnabled; },
	        template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/widgets/integrations/schedule-widget.tpl.html'
	    };
	    widgetService.registerWidget(scheduleWidget, 'subscriptionDetail');
	    // integration: schedule in product details
	    widgetService.registerWidget(scheduleWidget, 'itemDetail');

	    // integration: subscription in order details
	    widgetService.registerWidget({
	        controller: 'virtoCommerce.subscriptionModule.orderSubscriptionWidgetController',
	        // visible only if the order was loaded from orderAPI and not from subscriptionAPI
	        isVisible: function (blade) { return blade.id === "orderDetail"; },
	        template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/widgets/integrations/order-subscription-widget.tpl.html'
	    }, 'customerOrderDetailWidgets');
	}]);
