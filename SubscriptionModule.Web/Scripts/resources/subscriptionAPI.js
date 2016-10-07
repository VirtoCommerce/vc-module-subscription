angular.module('virtoCommerce.subscriptionModule')
.factory('virtoCommerce.subscriptionModule.subscriptionAPI', ['$resource', function ($resource) {
    return $resource('api/order/customerOrders/:id', { id: '@id' }, {
    //return $resource('api/subscriptions/:id', { id: '@id' }, {
        search: { method: 'POST', url: 'api/order/customerOrders/search' },
        update: { method: 'PUT' }
    });
}])
.factory('virtoCommerce.subscriptionModule.scheduleAPI', ['$resource', function ($resource) {
    return $resource('api/order/customerOrders/:id', { id: '@id' }, {
        //return $resource('api/subscriptions/schedules/:id', { id: '@id' }, {
        getScheduleForProduct: { url: 'api/catalog/products/:id' },
        deleteSchedule: {},
        update: { method: 'PUT' }
    });
}]);

