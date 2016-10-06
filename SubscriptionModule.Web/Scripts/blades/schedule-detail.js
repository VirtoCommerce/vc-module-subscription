angular.module('virtoCommerce.subscriptionModule')
.controller('virtoCommerce.subscriptionModule.scheduleDetailController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    blade.initialize = function (data) {
        blade.origEntity = data;
        blade.currentEntity = angular.copy(data);
        blade.isLoading = false;
    };

    $scope.frequencyMeasures = ['days', 'months'];

    function isDirty() {
        return !angular.equals(blade.currentEntity, blade.origEntity) && blade.hasUpdatePermission();
    }

    function canSave() {
        return isDirty() && $scope.formScope.$valid;
    }

    $scope.setForm = function (form) { $scope.formScope = form; };

    blade.onClose = function (closeCallback) {
        bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, $scope.saveChanges, closeCallback, "subscription.dialogs.schedule-save.title", "subscription.dialogs.schedule-save.message");
    };

    $scope.cancelChanges = function () {
        blade.currentEntity = blade.origEntity;
        $scope.bladeClose();
    };
    $scope.saveChanges = function () {
        if (blade.isApiSave) {
            blade.isLoading = true;
            //subscriptionAPI.save???({ id: blade.currentEntity.objectType, propertyId: blade.currentEntity.id },
            //    blade.currentEntities,
            //    function () {
            //        refresh();
            //        if (blade.onChangesConfirmedFn)
            //            blade.onChangesConfirmedFn();
            //    });
            $scope.bladeClose();
        } else {
            angular.copy(blade.currentEntity, blade.origEntity);
            $scope.bladeClose();
        }
    };

    blade.toolbarCommands = [
        {
            name: "platform.commands.save", icon: 'fa fa-save',
            executeMethod: $scope.saveChanges,
            canExecuteMethod: canSave,
            permission: blade.updatePermission
        },
        {
            name: "platform.commands.reset", icon: 'fa fa-undo',
            executeMethod: function () {
                angular.copy(blade.origEntity, blade.currentEntity);
            },
            canExecuteMethod: isDirty,
            permission: blade.updatePermission
        }
    ];

    if (!blade.isApiSave) {
        $scope.blade.toolbarCommands.splice(0, 1); // remove save button
    }

    angular.extend(blade, {
        title: 'subscription.blades.schedule-detail.title'
        //titleValues: { customer: blade.mainEntity.customerName },
        //subtitle: 'subscription.blades.subscription-detail.subtitle'
    });

    blade.initialize(blade.data);
}]);