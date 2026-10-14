<template>
  <VcBlade
    :title="$t('SUBSCRIPTIONS.PAGES.PLAN.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="40%"
  >
    <VcContainer>
      <VcForm class="subscriptions-form tw-space-y-4">
        <VcCard :header="$t('SUBSCRIPTIONS.DETAILS.BILLING')">
          <div class="subscriptions-card">
            <VcHint>{{ $t("SUBSCRIPTIONS.PAGES.PLAN.HINT") }}</VcHint>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcInput
                  :model-value="plan.intervalCount ?? 1"
                  :label="$t('SUBSCRIPTIONS.FIELDS.INTERVAL_COUNT')"
                  type="integer"
                  required
                  @update:model-value="(v) => (plan.intervalCount = Number(v) || 1)"
                />
              </VcCol>
              <VcCol>
                <VcSelect
                  v-model="plan.interval"
                  :label="$t('SUBSCRIPTIONS.FIELDS.INTERVAL')"
                  :options="intervals"
                  :clearable="false"
                  required
                />
              </VcCol>
            </VcRow>
            <VcInput
              :model-value="plan.trialPeriodDays ?? 0"
              :label="$t('SUBSCRIPTIONS.FIELDS.TRIAL_DAYS')"
              type="integer"
              @update:model-value="(v) => (plan.trialPeriodDays = Number(v) || 0)"
            />
          </div>
        </VcCard>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useApiClient, useAsync, useBlade, useBladeForm, useLoading, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCard, VcCol, VcContainer, VcForm, VcHint, VcInput, VcRow, VcSelect } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { PaymentInterval, SubscriptionModuleClient, type PaymentPlan } from "../../api_client/virtocommerce.subscription";

// The auto-order schedule of a product (param = product id): orders of the product become a subscription
defineBlade({
  url: "/payment-plan",
  name: "SubscriptionPaymentPlan",
});

const { t } = useI18n({ useScope: "global" });
const { param, closeSelf } = useBlade();
const { showConfirmation } = usePopup();
const { getApiClient } = useApiClient(SubscriptionModuleClient);

const plan = ref<PaymentPlan>({});
const exists = ref(false);
const intervals = Object.values(PaymentInterval).map((x) => ({ id: x, title: x }));
const form = useBladeForm({ data: plan, closeConfirmMessage: computed(() => t("SUBSCRIPTIONS.ALERTS.CLOSE_CONFIRMATION")) });

const { loading: fetching, action: load } = useAsync(async () => {
  // the API answers an empty body when the product has no plan
  const existing = await (await getApiClient()).getPaymentPlanById(param.value!).catch(() => null);
  exists.value = !!existing?.id;
  plan.value = exists.value ? existing! : { id: param.value, intervalCount: 1, interval: PaymentInterval.Months, trialPeriodDays: 0 };
  form.setBaseline();
});

const { loading: saving, action: save } = useAsync(async () => {
  await (await getApiClient()).createPaymentPlan(plan.value); // POST upserts
  await load();
});

const loading = useLoading(fetching, saving);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("SUBSCRIPTIONS.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "paymentplan:manage",
    // a new plan can be saved as it is
    disabled: computed(() => (exists.value && !form.canSave.value) || !plan.value.intervalCount),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("SUBSCRIPTIONS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
  {
    id: "delete",
    title: t("SUBSCRIPTIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "paymentplan:manage",
    disabled: computed(() => !exists.value),
    async clickHandler() {
      if (await showConfirmation(t("SUBSCRIPTIONS.ALERTS.DELETE_PLAN"))) {
        await (await getApiClient()).deletePlansByIds([param.value!]);
        form.setBaseline();
        await closeSelf();
      }
    },
  },
]);

onMounted(load);
</script>
