angular.module('virtoCommerce.subscriptionModule')
.factory('virtoCommerce.subscriptionModule.subscriptionAPI', ['$resource', function ($resource) {
    return $resource('api/subscriptions/:id', null, {
        search: { method: 'POST', url: 'api/subscriptions/search' },
        update: { method: 'PUT' }
    });
}])
.factory('virtoCommerce.subscriptionModule.scheduleAPI', ['$resource', function ($resource) {
    return $resource('api/subscriptions/plans/:id');
}]);
