angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.scheduleWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.subscriptionModule.scheduleAPI', function ($scope, bladeNavigationService, scheduleAPI) {
    var blade = $scope.blade;
    var isApiSave = blade.id !== 'subscriptionDetail';

    function refresh() {
        if (isApiSave) {
            $scope.loading = true;
            scheduleAPI.getScheduleForProduct({ id: blade.itemId }, function (data) {
                $scope.loading = false;
                if (data && data.productType === "Physical") {
                    $scope.schedule = { frequency: 2, frequencyMeasure: 'months' };
                }
            });
        } else {
            $scope.schedule = { frequency: 3, frequencyMeasure: 'months' };
        }
    }

    $scope.openBlade = function () {
        var newBlade = {
            id: 'orderOperationChild',
            isApiSave: isApiSave,
            data: $scope.schedule,
            controller: 'virtoCommerce.subscriptionModule.scheduleDetailController',
            template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/blades/schedule-detail.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    };

    refresh();
}]);
