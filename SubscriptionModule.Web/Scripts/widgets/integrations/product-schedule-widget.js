angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.productScheduleWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.subscriptionModule.subscriptionAPI', function ($scope, bladeNavigationService, subscriptionAPI) {
    var blade = $scope.blade;

    function refresh() {
        $scope.loading = true;
        return subscriptionAPI.getScheduleForProduct({ id: blade.itemId }, function (data) {
            $scope.loading = false;
            if (data && data.productType === "Physical") {
                $scope.schedule = { frequency: 2, frequencyMeasure : 'month'};
            }
        });
    }

    $scope.openBlade = function () {
        var newBlade = {
            id: 'orderOperationChild',
            isApiSave: true,
            data: $scope.schedule,
            controller: 'virtoCommerce.subscriptionModule.scheduleDetailController',
            template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/blades/schedule-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };

    refresh();
}]);
