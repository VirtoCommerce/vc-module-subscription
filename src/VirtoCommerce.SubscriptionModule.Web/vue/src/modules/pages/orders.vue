<template>
  <VcBlade
    :title="$t('SUBSCRIPTIONS.WIDGETS.ORDERS')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <!-- the orders created by the subscription; an order opens in the Orders module's blade -->
    <VcDataTable
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :state-key="'SUBSCRIPTION_ORDERS'"
      :active-item-id="activeChildParam"
      :empty-state="{ icon: 'lucide-file-text', title: $t('SUBSCRIPTIONS.PAGES.ORDERS.EMPTY') }"
      @row-click="(e: { data: Order }) => openOrder(e.data.id)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="number"
        :title="$t('SUBSCRIPTIONS.PAGES.ORDERS.NUMBER')"
        always-visible
      />
      <VcColumn
        id="createdDate"
        :title="$t('SUBSCRIPTIONS.FIELDS.CREATED')"
        type="date"
      />
      <VcColumn
        id="status"
        :title="$t('SUBSCRIPTIONS.FIELDS.STATUS')"
        always-visible
      >
        <template #body="{ data: o }">
          <div class="tw-inline-flex">
            <VcStatus :variant="o.isCancelled ? 'danger' : 'info'">{{ o.status }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="total"
        :title="$t('SUBSCRIPTIONS.FIELDS.TOTAL')"
        align="end"
        always-visible
      >
        <template #body="{ data: o }">
          <span class="vc-table-cell-money">{{ n(o.total ?? 0, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} {{ o.currency }}</span>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useDataTablePagination, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { post, type SearchResult } from "../api";

defineBlade({
  url: "/subscription-orders",
  name: "SubscriptionOrders",
});

interface Order {
  id: string;
  number?: string;
  status?: string;
  isCancelled?: boolean;
  total?: number;
  currency?: string;
  createdDate?: string;
}

const { t, n } = useI18n({ useScope: "global" });
const { param, activeChildParam, openBlade, callParent, exposeToChildren } = useBlade();

const PAGE_SIZE = 20;
const items = ref<Order[]>([]);
const totalCount = ref(0);
const pagination = useDataTablePagination({ pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const result = await post<SearchResult<Order>>("/api/order/customerOrders/search", {
    subscriptionIds: [param.value],
    sort: "createdDate:desc",
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = (result.results ?? []).map((o) => ({ ...o, createdDate: o.createdDate && (new Date(o.createdDate) as unknown as string) }));
  totalCount.value = result.totalCount ?? 0;
});

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SUBSCRIPTIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

function openOrder(id: string) {
  openBlade({ name: "OrdersDetails", param: id });
}

watch(() => pagination.skip, () => load());
onMounted(load);

// a saved order changes the subscription's balance
exposeToChildren({
  async reload() {
    await load();
    await callParent("reload");
  },
});
</script>
