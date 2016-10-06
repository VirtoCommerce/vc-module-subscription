angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.subscriptionOrdersWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', '$timeout', function ($scope, bladeNavigationService, $timeout) {
    var blade = $scope.blade;

    $scope.openBlade = function () {
        var newBlade = {
		    id: 'subsrciptionOrders',
		    // data: blade.currentEntity.childrenOperations,
		    title: 'orders.blades.customerOrder-list.title',
		    controller: 'virtoCommerce.orderModule.customerOrderListController',
		    template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/customerOrder-list.tpl.html'
		};
		bladeNavigationService.showBlade(newBlade, blade);

		$timeout(function(){
		    newBlade.filter.keyword = blade.currentEntity.number;
		    newBlade.filter.criteriaChanged();
		}, 100);
	};
	
}]);
