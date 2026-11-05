<template>
  <VcWidget
    icon="lucide-repeat"
    :title="$t('SUBSCRIPTIONS.WIDGETS.SUBSCRIPTION')"
    :value="order?.subscriptionNumber ?? '—'"
    :disabled="!order?.subscriptionId"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed } from "vue";
import { injectBladeContext, useBlade } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";

// The Subscriptions module extends the Orders module's blade, the way its AngularJS widget extends
// customerOrderDetailWidgets: registered by blade name, the order comes from the blade context.
const ctx = injectBladeContext();
const order = computed(() => ctx.value.item as { subscriptionId?: string; subscriptionNumber?: string } | undefined);
const { openBlade } = useBlade();

// fromOrder: the subscription blade then leaves this order blade (and its unsaved edits) alone
const open = () => openBlade({ name: "SubscriptionDetails", param: order.value?.subscriptionId, options: { fromOrder: true } });
</script>
