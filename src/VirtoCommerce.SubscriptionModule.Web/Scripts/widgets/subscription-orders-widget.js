angular.module('virtoCommerce.subscriptionModule')
    .controller('virtoCommerce.subscriptionModule.subscriptionOrdersWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.orderModule.order_res_customerOrders',
        function ($scope, bladeNavigationService, orders) {
            var blade = $scope.blade;

            $scope.openBlade = function () {
                if (!blade.isLoading) {
                    var newBlade = {
                        id: 'subscriptionOrders',
                        title: 'subscription.blades.subscriptionOrder-list.title',
                        controller: 'virtoCommerce.orderModule.customerOrderListController',
                        template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/customerOrder-list.tpl.html',
                        hideDelete: true
                    };

                    if (blade.currentEntity.id) {
                        newBlade.refreshCallback = function () {
                            var criteria = {
                                subscriptionIds: [blade.currentEntity.id]
                            };

                            return orders.search(criteria);
                        }
                    }

                    bladeNavigationService.showBlade(newBlade, blade);
                }
            };

        }]);
