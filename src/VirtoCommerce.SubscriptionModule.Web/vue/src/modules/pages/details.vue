<template>
  <VcBlade
    :title="title"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="70%"
  >
    <VcContainer>
      <VcForm class="subscriptions-form tw-space-y-4">
        <VcCard
          v-if="cancelling"
          :header="$t('SUBSCRIPTIONS.CANCEL.TITLE')"
        >
          <div class="subscriptions-card">
            <VcTextarea
              v-model="cancelReason"
              :label="$t('SUBSCRIPTIONS.CANCEL.REASON')"
              :placeholder="$t('SUBSCRIPTIONS.CANCEL.PLACEHOLDER')"
            />
            <div class="subscriptions-actions">
              <VcButton
                variant="secondary"
                @click="cancelling = false"
              >
                {{ $t("SUBSCRIPTIONS.CANCEL.KEEP") }}
              </VcButton>
              <VcButton @click="confirmCancel">{{ $t("SUBSCRIPTIONS.CANCEL.CONFIRM") }}</VcButton>
            </div>
          </div>
        </VcCard>

        <VcRow class="tw-gap-4">
          <VcCol>
            <VcInput
              v-model="item.number"
              :label="$t('SUBSCRIPTIONS.FIELDS.NUMBER')"
              :disabled="locked"
              required
            />
          </VcCol>
          <VcCol>
            <div class="subscriptions-field">
              <VcLabel>{{ $t("SUBSCRIPTIONS.FIELDS.STATUS") }}</VcLabel>
              <div class="tw-inline-flex">
                <VcStatus :variant="statusVariant(item.subscriptionStatus)">{{ item.subscriptionStatus }}</VcStatus>
              </div>
            </div>
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('SUBSCRIPTIONS.FIELDS.CUSTOMER')"
              :model-value="item.customerName"
            />
          </VcCol>
          <VcCol>
            <VcSelect
              v-model="item.storeId"
              :label="$t('SUBSCRIPTIONS.FIELDS.STORE')"
              :options="searchStores"
              option-label="name"
              :clearable="false"
              :disabled="locked"
              searchable
              required
            />
          </VcCol>
        </VcRow>
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcField
              :label="$t('SUBSCRIPTIONS.FIELDS.BALANCE')"
              :model-value="money(item.balance)"
            />
          </VcCol>
          <VcCol>
            <VcField
              :label="$t('SUBSCRIPTIONS.FIELDS.CREATED')"
              :model-value="item.createdDate"
              type="date"
            />
          </VcCol>
        </VcRow>
        <VcCard
          v-if="item.isCancelled"
          :header="$t('SUBSCRIPTIONS.FIELDS.CANCELLED')"
          variant="danger"
        >
          <div class="subscriptions-card">
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcField
                  :label="$t('SUBSCRIPTIONS.FIELDS.CANCELLED_DATE')"
                  :model-value="item.cancelledDate"
                  type="date"
                />
              </VcCol>
              <VcCol>
                <VcField
                  :label="$t('SUBSCRIPTIONS.CANCEL.REASON')"
                  :model-value="item.cancelReason || '—'"
                />
              </VcCol>
            </VcRow>
          </div>
        </VcCard>

        <VcCard :header="$t('SUBSCRIPTIONS.DETAILS.BILLING')">
          <div class="subscriptions-card">
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcInput
                  :model-value="item.intervalCount ?? 1"
                  :label="$t('SUBSCRIPTIONS.FIELDS.INTERVAL_COUNT')"
                  type="integer"
                  :disabled="locked"
                  required
                  @update:model-value="(v) => (item.intervalCount = Number(v) || 1)"
                />
              </VcCol>
              <VcCol>
                <VcSelect
                  v-model="item.interval"
                  :label="$t('SUBSCRIPTIONS.FIELDS.INTERVAL')"
                  :options="intervals"
                  :clearable="false"
                  :disabled="locked"
                  required
                />
              </VcCol>
              <VcCol>
                <VcInput
                  :model-value="item.trialPeriodDays ?? 0"
                  :label="$t('SUBSCRIPTIONS.FIELDS.TRIAL_DAYS')"
                  type="integer"
                  :disabled="locked"
                  @update:model-value="(v) => (item.trialPeriodDays = Number(v) || 0)"
                />
              </VcCol>
            </VcRow>
            <VcRow class="tw-gap-4">
              <VcCol>
                <VcInput
                  v-model="item.currentPeriodEnd"
                  :label="$t('SUBSCRIPTIONS.FIELDS.NEXT_BILLING')"
                  type="datetime-local"
                  :disabled="locked"
                />
              </VcCol>
              <VcCol>
                <VcInput
                  v-model="item.endDate"
                  :label="$t('SUBSCRIPTIONS.FIELDS.END_DATE')"
                  type="datetime-local"
                  :disabled="locked"
                />
              </VcCol>
              <VcCol>
                <VcInput
                  v-model="item.trialEnd"
                  :label="$t('SUBSCRIPTIONS.FIELDS.TRIAL_END')"
                  type="datetime-local"
                  :disabled="locked"
                />
              </VcCol>
            </VcRow>
            <VcTextarea
              v-model="item.comment"
              :label="$t('SUBSCRIPTIONS.FIELDS.COMMENT')"
              :disabled="locked"
            />
          </div>
        </VcCard>

        <!-- the order every recurring order is made from; it is edited in the Orders blade (widget "Order prototype") -->
        <VcCard :header="$t('SUBSCRIPTIONS.DETAILS.PROTOTYPE')">
          <VcDataTable
            :items="item.customerOrderPrototype?.items ?? []"
            :state-key="'SUBSCRIPTION_PROTOTYPE_ITEMS'"
          >
            <VcColumn
              id="name"
              :title="$t('SUBSCRIPTIONS.FIELDS.ITEM')"
            />
            <VcColumn
              id="sku"
              :title="$t('SUBSCRIPTIONS.FIELDS.SKU')"
            />
            <VcColumn
              id="quantity"
              :title="$t('SUBSCRIPTIONS.FIELDS.QUANTITY')"
              type="number"
            />
            <VcColumn
              id="extendedPrice"
              :title="$t('SUBSCRIPTIONS.FIELDS.TOTAL')"
              align="end"
            >
              <template #body="{ data: line }">
                <span class="vc-table-cell-money">{{ money(line.extendedPrice) }}</span>
              </template>
            </VcColumn>
          </VcDataTable>
          <dl class="subscriptions-totals">
            <dt>{{ $t("SUBSCRIPTIONS.FIELDS.TOTAL") }}</dt>
            <dd>{{ money(item.customerOrderPrototype?.total) }}</dd>
          </dl>
        </VcCard>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  defineBladeContext,
  notification,
  useApiClient,
  useAsync,
  useBlade,
  useBladeForm,
  useBladeWidgets,
  useLoading,
  usePopup,
  type IBladeToolbar,
} from "@vc-shell/framework";
import {
  VcBlade,
  VcButton,
  VcCard,
  VcCol,
  VcColumn,
  VcContainer,
  VcDataTable,
  VcField,
  VcForm,
  VcInput,
  VcLabel,
  VcRow,
  VcSelect,
  VcStatus,
  VcTextarea,
} from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { PaymentInterval, SubscriptionModuleClient, type Subscription } from "../../api_client/virtocommerce.subscription";
import { post, statusVariant, type SearchResult } from "../api";

defineBlade({
  url: "/subscription",
  name: "SubscriptionDetails",
});

const { t, n } = useI18n({ useScope: "global" });
const { param, options, openBlade, callParent: callOwner, closeSelf, exposeToChildren } = useBlade<{ fromOrder?: boolean }>();
// opened from the order's Subscription widget: the parent is the order blade, whose reload would drop its unsaved edits
// (and nothing here changes that order), so only a subscriptions list parent is reloaded
const callParent = (method: string) => (options.value?.fromOrder ? Promise.resolve() : callOwner(method));
const { showConfirmation } = usePopup();
const { getApiClient } = useApiClient(SubscriptionModuleClient);

const item = ref<Subscription>({});
const ordersCount = ref<number | string>("");
const cancelling = ref(false);
const cancelReason = ref("");
const locked = computed(() => !!item.value.isCancelled);
const intervals = Object.values(PaymentInterval).map((x) => ({ id: x, title: x }));
const title = computed(() => t("SUBSCRIPTIONS.DETAILS.TITLE", { number: item.value.number ?? "" }));

const form = useBladeForm({
  data: item,
  canSaveOverride: computed(() => !!item.value.number && !!item.value.storeId),
  closeConfirmMessage: computed(() => t("SUBSCRIPTIONS.ALERTS.CLOSE_CONFIRMATION")),
});

const money = (value?: number) =>
  `${n(value ?? 0, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${item.value.customerOrderPrototype?.currency ?? ""}`;

async function searchStores(keyword?: string, skip = 0, ids?: string[]) {
  return post<SearchResult<{ id: string; name: string }>>("/api/stores/search", { keyword, objectIds: ids, skip, take: ids?.length || 20 });
}

// the subscription as last loaded: what differs from it are the user's unsaved edits
let loaded: Record<string, unknown> = {};

const { loading: fetching, action: load } = useAsync(async () => {
  const [subscription, orders] = await Promise.all([
    (await getApiClient()).getSubscriptionById(param.value!),
    post<SearchResult<unknown>>("/api/order/customerOrders/search", { subscriptionIds: [param.value], take: 0 }).catch(() => undefined),
  ]);
  item.value = subscription;
  form.setBaseline();
  loaded = JSON.parse(JSON.stringify(item.value));
  ordersCount.value = orders?.totalCount ?? "";
});

const { loading: saving, action: save } = useAsync(async () => {
  await (await getApiClient()).updateSubscription(item.value);
  await load();
  callParent("reload");
});

const { loading: cancelLoading, action: cancel } = useAsync(async () => {
  const client = await getApiClient();
  // pending edits are saved with the cancel, as the AngularJS blade did (it saved the entity marked cancelled)
  if (form.isModified.value) await client.updateSubscription(item.value);
  await client.cancelSubscription({ subscriptionId: item.value.id, cancelReason: cancelReason.value.trim() || undefined });
  cancelling.value = false;
  await load();
  callParent("reload");
});

const loading = useLoading(fetching, saving, cancelLoading);

async function confirmCancel() {
  if (await showConfirmation(t("SUBSCRIPTIONS.ALERTS.CANCEL_CONFIRMATION", { number: item.value.number }))) await cancel();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: t("SUBSCRIPTIONS.TOOLBAR.SAVE"),
    icon: "lucide-save",
    permissions: "subscription:update",
    disabled: computed(() => !form.canSave.value || locked.value),
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
    id: "createOrder",
    title: t("SUBSCRIPTIONS.TOOLBAR.CREATE_ORDER"),
    icon: "lucide-file-plus",
    permissions: "subscription:update",
    disabled: computed(() => locked.value || form.isModified.value),
    async clickHandler() {
      if (await showConfirmation(t("SUBSCRIPTIONS.ALERTS.CREATE_ORDER_CONFIRMATION"))) {
        await (await getApiClient()).createRecurrentOrderForSubscription(item.value);
        await load();
        callParent("reload");
      }
    },
  },
  {
    id: "cancel",
    title: t("SUBSCRIPTIONS.TOOLBAR.CANCEL"),
    icon: "lucide-ban",
    permissions: "subscription:update",
    disabled: computed(() => locked.value || cancelling.value),
    clickHandler: () => ((cancelReason.value = ""), (cancelling.value = true)),
  },
  {
    id: "delete",
    title: t("SUBSCRIPTIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "subscription:delete",
    async clickHandler() {
      if (await showConfirmation(t("SUBSCRIPTIONS.ALERTS.DELETE_SELECTED", { count: 1 }))) {
        await (await getApiClient()).deleteSubscriptionsByIds([item.value.id!]);
        form.setBaseline();
        callParent("reload");
        await closeSelf();
      }
    },
  },
]);

useBladeWidgets([
  {
    id: "SubscriptionOrders",
    icon: "lucide-file-text",
    title: "SUBSCRIPTIONS.WIDGETS.ORDERS",
    badge: ordersCount,
    onClick: () => openBlade({ name: "SubscriptionOrders", param: item.value.id, options: { number: item.value.number } }),
  },
  {
    // the Orders module's order blade edits the prototype: items, addresses, payments, shipments
    id: "SubscriptionOrderPrototype",
    icon: "lucide-file-cog",
    title: "SUBSCRIPTIONS.WIDGETS.PROTOTYPE",
    disabled: computed(() => !item.value.customerOrderPrototypeId),
    onClick: () => openBlade({ name: "OrdersDetails", param: item.value.customerOrderPrototypeId }),
  },
  {
    id: "SubscriptionChanges",
    icon: "lucide-history",
    title: "SUBSCRIPTIONS.WIDGETS.CHANGES",
    onClick: () => openBlade({ name: "SubscriptionChanges", param: item.value.id }),
  },
  {
    // the Notifications module's journal, as the AngularJS widget opened it
    id: "SubscriptionNotificationLog",
    icon: "lucide-mail",
    title: "SUBSCRIPTIONS.WIDGETS.NOTIFICATION_LOG",
    onClick: () =>
      openBlade({
        name: "NotificationsJournal",
        options: { objectId: item.value.id, objectType: "Subscription", title: item.value.number },
      }).catch(() => notification.error(t("SUBSCRIPTIONS.ALERTS.NOTIFICATIONS_UNAVAILABLE"), { timeout: 5000 })),
  },
]);

// the subscription, for widgets other modules may add to this blade
defineBladeContext({ item });

onMounted(load);

// A child saved (the prototype order, an order of the subscription): re-read the subscription and put the user's
// unsaved edits back on top — a Save with the stale prototype would revert the prototype order. A field both
// changed keeps the user's value.
async function reloadKeepingEdits() {
  const current = item.value as Record<string, unknown>;
  const keys = new Set([...Object.keys(current), ...Object.keys(loaded)]);
  const edits = [...keys].filter((k) => JSON.stringify(current[k]) !== JSON.stringify(loaded[k]));
  const values = Object.fromEntries(edits.map((k) => [k, current[k]]));
  await load();
  Object.assign(item.value, values);
}

exposeToChildren({
  async reload() {
    await (form.isModified.value ? reloadKeepingEdits() : load());
    callParent("reload");
  },
});
</script>

<style lang="scss">
.subscriptions-form {
  padding: 20px 22px 36px;
}

.subscriptions-card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.subscriptions-field {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
}

.subscriptions-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.subscriptions-totals {
  font-size: 14px;
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 8px 16px;
  margin: 0;
  padding: 16px;
  border-top: 1px solid var(--neutrals-200);
  font-weight: 700;
  color: var(--neutrals-950);
  dd {
    margin: 0;
    text-align: right;
    font-variant-numeric: tabular-nums;
  }
}
</style>
