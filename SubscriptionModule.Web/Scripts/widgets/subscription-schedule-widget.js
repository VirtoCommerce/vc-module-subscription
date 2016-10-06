angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.subscriptionScheduleWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    $scope.openBlade = function () {
		var newBlade = {
			id: 'orderOperationChild',
			data: blade.currentEntity,
			controller: 'virtoCommerce.subscriptionModule.scheduleDetailController',
			template: 'Modules/$(VirtoCommerce.Subscription)/Scripts/blades/schedule-detail.tpl.html'
		};
		bladeNavigationService.showBlade(newBlade, blade);
	};
	
}]);
