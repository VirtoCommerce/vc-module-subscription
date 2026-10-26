import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, registerExternalWidget, usePermissions } from "@vc-shell/framework";
import { markRaw } from "vue";
import OrderSubscriptionWidget from "./widgets/OrderSubscriptionWidget.vue";
import ProductPaymentPlanWidget from "./widgets/ProductPaymentPlanWidget.vue";

// The Subscriptions module extends other modules' blades, as its AngularJS widgets do:
// the order's subscription (customerOrderDetailWidgets) and the product's auto-order schedule (itemDetail).
registerExternalWidget({
  id: "OrderSubscription",
  title: "Subscription",
  component: markRaw(OrderSubscriptionWidget),
  targetBlades: ["OrdersDetails"],
  // with subscription:read, as the AngularJS widget
  isVisible: () => usePermissions().hasAccess("subscription:read"),
});
registerExternalWidget({
  id: "ProductPaymentPlan",
  title: "Payment plan",
  component: markRaw(ProductPaymentPlanWidget),
  targetBlades: ["CatalogProductDetails"],
});

export default defineAppModule({
  blades,
  locales,
});
