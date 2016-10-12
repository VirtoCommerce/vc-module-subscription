angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.subscriptionListController', ['$scope', '$localStorage', 'virtoCommerce.subscriptionModule.subscriptionAPI', 'virtoCommerce.orderModule.knownOperations', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper', 'platformWebApp.bladeUtils', 'dateFilter',
function ($scope, $localStorage, subscriptionAPI, knownOperations, bladeNavigationService, dialogService, uiGridHelper, bladeUtils, dateFilter) {
    $scope.uiGridConstants = uiGridHelper.uiGridConstants;
    var blade = $scope.blade;

    blade.refresh = function () {
        blade.isLoading = true;
        var criteria = {
            keyword: filter.keyword,
            sort: uiGridHelper.getSortExpression($scope),
            skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
            take: $scope.pageSettings.itemsPerPageCount
        };
        if (filter.current) {
            angular.extend(criteria, filter.current);
        }

        subscriptionAPI.search(criteria, function (data) {
            $scope.pageSettings.totalItems = data.totalCount;
            blade.currentEntities = data.subscriptions;

            blade.isLoading = false;
        });
    };

    $scope.openDetailsBlade = function (node) {
        $scope.selectedNodeId = node.id;

        var foundTemplate = knownOperations.getOperation('Subscription');
        var newBlade = angular.copy(foundTemplate.detailBlade);
        newBlade.entityNode = node;
        bladeNavigationService.showBlade(newBlade, blade);
    };

    $scope.deleteList = function (list) {
        var dialog = {
            id: "confirmDelete",
            callback: function (remove) {
                if (remove) {
                    bladeNavigationService.closeChildrenBlades(blade, function () {
                        $scope.selectedNodeId = undefined;
                        blade.isLoading = true;

                        var ids = _.pluck(list, 'id');
                        subscriptionAPI.remove({ ids: ids }, blade.refresh);
                    });
                }
            }
        }
        dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Subscription)/Scripts/dialogs/deleteSubscription-dialog.tpl.html', 'platformWebApp.confirmDialogController');
    };

    blade.headIcon = 'fa-retweet';

    blade.toolbarCommands = [
        {
            name: "platform.commands.refresh", icon: 'fa fa-refresh',
            executeMethod: blade.refresh,
            canExecuteMethod: function () { return true; }
        },
        {
            name: "platform.commands.delete", icon: 'fa fa-trash-o',
            executeMethod: function () {
                $scope.deleteList($scope.gridApi.selection.getSelectedRows());
            },
            canExecuteMethod: function () {
                return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
            },
            permission: 'subscription:delete'
        }
    ];

    // simple and advanced filtering
    var filter = blade.filter = $scope.filter = {};
    $scope.subscriptionSearchFilters = [
        { id: 'trial', name: 'subscription.blades.subscription-list.labels.filter-trialling' },
        { id: 'active', name: 'subscription.blades.subscription-list.labels.filter-active' },
        { id: 'passed', name: 'subscription.blades.subscription-list.labels.filter-passed' },
        { id: 'aboutToExpire', name: 'subscription.blades.subscription-list.labels.filter-expires-soon' },
        { id: 'canceled', name: 'subscription.blades.subscription-list.labels.filter-canceled' },
        { id: 'withIssues', name: 'subscription.blades.subscription-list.labels.filter-with-issues' }
    ];

    if ($localStorage.subscriptionSearchFilterId) {
        filter.current = _.findWhere($scope.subscriptionSearchFilters, { id: $localStorage.subscriptionSearchFilterId });
    }

    filter.change = function () {
        $localStorage.subscriptionSearchFilterId = filter.current ? filter.current.id : null;
        filter.criteriaChanged();
    };

    filter.criteriaChanged = function () {
        if ($scope.pageSettings.currentPage > 1) {
            $scope.pageSettings.currentPage = 1;
        } else {
            blade.refresh();
        }
    };

    // ui-grid
    $scope.setGridOptions = function (gridOptions) {
        var createdDateColumn = _.findWhere(gridOptions.columnDefs, { name: 'createdDate' });
        if (createdDateColumn) { // custom tooltip
            createdDateColumn.cellTooltip = function (row, col) { return dateFilter(row.entity.createdDate, 'medium'); }
        }
        uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
            uiGridHelper.bindRefreshOnSortChanged($scope);
        });

        bladeUtils.initializePagination($scope);
    };

    // actions on load
    // blade.refresh();
}]);