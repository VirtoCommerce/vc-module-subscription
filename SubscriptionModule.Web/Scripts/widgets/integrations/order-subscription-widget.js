angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.orderSubscriptionWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.orderModule.knownOperations', 'virtoCommerce.subscriptionModule.subscriptionAPI', function ($scope, bladeNavigationService, knownOperations, subscriptionAPI) {
    var blade = $scope.blade;
    $scope.loading = true;

    subscriptionAPI.search({ customerOrderId: blade.customerOrder.id }, function (data) {
        if (data.totalCount) {
            $scope.subscription = data.subscriptions[0];
        }
        $scope.loading = false;
    });

    $scope.openBlade = function () {
        if ($scope.subscription) {
            var subscriptionDetailBlade = bladeNavigationService.findBlade('subscriptionDetail');
            if (subscriptionDetailBlade) {
                bladeNavigationService.closeBlade(blade.parentBlade);
            } else {
                var foundTemplate = knownOperations.getOperation('Subscription');
                var newBlade = angular.copy(foundTemplate.detailBlade);
                newBlade.entityNode = $scope.subscription;
                bladeNavigationService.showBlade(newBlade, blade);
            }
        }
    };
}]);
