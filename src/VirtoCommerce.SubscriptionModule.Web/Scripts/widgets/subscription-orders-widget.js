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
                        isExpandable: true,
                        hideDelete: true,
                    };

                    if (blade.currentEntity.id) {
                        newBlade.refreshCallback = function (orderBlade, orderBladeCriteria) {
                            var criteria = {
                                subscriptionIds: [blade.currentEntity.id],
                                sort: orderBladeCriteria.sort,
                                skip: orderBladeCriteria.skip,
                                take: orderBladeCriteria.take
                            };

                            return orders.search(criteria);
                        }
                    }

                    bladeNavigationService.showBlade(newBlade, blade);
                }
            };

            function refresh() {
                $scope.ordersCount = '...';

                var countSearchCriteria = {
                    subscriptionIds: [blade.entityNode.id],
                    take: 0
                };

                orders.search(countSearchCriteria, function (data) {
                    $scope.ordersCount = data.totalCount;
                });
            }

            refresh();
        }]);
