<template>
  <VcWidget
    icon="lucide-calendar-sync"
    :title="$t('SUBSCRIPTIONS.WIDGETS.PAYMENT_PLAN')"
    :value="plan ? `${plan.intervalCount} ${plan.interval}` : '—'"
    :disabled="!productId"
    @click="open"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { injectBladeContext, useApiClient, useBlade, useWidgetTrigger } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { SubscriptionModuleClient, type PaymentPlan } from "../../api_client/virtocommerce.subscription";

// The auto-order schedule widget the AngularJS module puts on the product blade (itemDetail);
// the Catalog plugin shares the product via defineBladeContext({ item })
const ctx = injectBladeContext();
const productId = computed(() => (ctx.value.item as { id?: string } | undefined)?.id);
const { openBlade } = useBlade();
const { getApiClient } = useApiClient(SubscriptionModuleClient);
const plan = ref<PaymentPlan | null>(null);

async function load() {
  const found = productId.value ? await (await getApiClient()).getPaymentPlanById(productId.value).catch(() => null) : null;
  plan.value = found?.id ? found : null;
}
watch(productId, load, { immediate: true });
useWidgetTrigger({ onRefresh: load });

const open = () => openBlade({ name: "SubscriptionPaymentPlan", param: productId.value, onClose: load });
</script>
