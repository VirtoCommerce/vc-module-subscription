angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.orderSubscriptionWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.orderModule.knownOperations', function ($scope, bladeNavigationService, knownOperations) {
    var blade = $scope.blade;

    $scope.openBlade = function () {
        var foundTemplate = knownOperations.getOperation('Subscription');
        var newBlade = angular.copy(foundTemplate.detailBlade);
        newBlade.customerOrder = blade.customerOrder;
        bladeNavigationService.showBlade(newBlade, blade);
    };
}]);
