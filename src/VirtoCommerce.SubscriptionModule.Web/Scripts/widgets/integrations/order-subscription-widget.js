angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.orderSubscriptionWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.orderModule.knownOperations', function ($scope, bladeNavigationService, knownOperations) {
    var blade = $scope.blade;

    $scope.$watch('widget.blade.customerOrder', function (operation) {
        $scope.subscription = {
            id: operation.subscriptionId,
            number: operation.subscriptionNumber
        };
    });

    $scope.openBlade = function () {
        if ($scope.subscription.id) {
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
