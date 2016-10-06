angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.subscriptionDetailController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    angular.extend(blade, {
        title: 'subscription.blades.subscription-detail.title',
        titleValues: { customer: blade.customerOrder.customerName },
        subtitle: 'subscription.blades.subscription-detail.subtitle'
    });
}]);