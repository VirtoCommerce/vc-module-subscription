<template>
  <VcBlade
    :title="$t('SUBSCRIPTIONS.PAGES.LIST.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      :state-key="'SUBSCRIPTIONS'"
      :active-item-id="activeChildParam"
      :show-all-columns="!activeChildParam"
      :global-filters="globalFilters"
      :empty-state="{ icon: 'lucide-repeat', title: $t('SUBSCRIPTIONS.PAGES.LIST.EMPTY') }"
      @row-click="(e: { data: Subscription }) => openDetails(e.data.id!)"
      @pagination-click="pagination.goToPage"
      @filter="onFilter"
    >
      <VcColumn
        id="number"
        :title="$t('SUBSCRIPTIONS.FIELDS.NUMBER')"
        body-class="subscriptions-list__number"
        sortable
        always-visible
      />
      <VcColumn
        id="customerName"
        :title="$t('SUBSCRIPTIONS.FIELDS.CUSTOMER')"
        sortable
      />
      <VcColumn
        id="storeId"
        :title="$t('SUBSCRIPTIONS.FIELDS.STORE')"
        sortable
      />
      <VcColumn
        id="startDate"
        :title="$t('SUBSCRIPTIONS.FIELDS.START_DATE')"
        type="date"
        sortable
      />
      <VcColumn
        id="subscriptionStatus"
        :title="$t('SUBSCRIPTIONS.FIELDS.STATUS')"
        sort-field="status"
        sortable
        always-visible
      >
        <template #body="{ data: s }">
          <div class="tw-inline-flex">
            <VcStatus :variant="statusVariant(s.subscriptionStatus)">{{ s.subscriptionStatus }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <!-- formatted here: the framework's money cell shows a 0 balance as "Not set" -->
      <VcColumn
        id="balance"
        :title="$t('SUBSCRIPTIONS.FIELDS.BALANCE')"
        align="end"
        always-visible
        sortable
      >
        <template #body="{ data: s }">
          <span class="vc-table-cell-money">{{ n(s.balance ?? 0, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</span>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import {
  useApiClient,
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  useLatestRequest,
  usePopup,
  useTableSearch,
  type IBladeToolbar,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SubscriptionModuleClient, SubscriptionStatus, type Subscription } from "../../api_client/virtocommerce.subscription";
import { statusVariant } from "../api";

defineBlade({
  url: "/subscriptions",
  name: "SubscriptionsList",
  isWorkspace: true,
  permissions: ["subscription:access"],
  menuItem: {
    title: "SUBSCRIPTIONS.MENU.TITLE",
    icon: "lucide-repeat",
    priority: 30,
  },
});

const { t, n } = useI18n({ useScope: "global" });
const { activeChildParam, openBlade, exposeToChildren, closeChildren } = useBlade();
const { debounce } = useFunctions();
const { showConfirmation } = usePopup();
const { getApiClient } = useApiClient(SubscriptionModuleClient);

const PAGE_SIZE = 20;
const items = ref<Subscription[]>([]);
const totalCount = ref(0);
const selection = ref<Subscription[]>([]);
const filters = ref<{ statuses?: string[]; startDate?: Date; endDate?: Date }>({});

const { sortField, sortOrder, sortExpression } = useDataTableSort({ stateKey: "SUBSCRIPTIONS", initialField: "createdDate", initialDirection: "DESC" });
const { searchValue } = useTableSearch({ stateKey: "SUBSCRIPTIONS" });
const pagination = useDataTablePagination({ stateKey: "SUBSCRIPTIONS", pageSize: PAGE_SIZE, totalCount });
const search = useLatestRequest();

const { loading, action: load } = useAsync(async () => {
  const result = await search.latest(
    (await getApiClient()).searchSubscriptions({
      keyword: searchValue.value || undefined,
      // the DB column of subscriptionStatus is Status
      sort: sortExpression.value?.replace(/^subscriptionStatus/, "status"),
      skip: pagination.skip,
      take: PAGE_SIZE,
      ...filters.value,
    }),
  );
  if (!result) return;
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

// statuses from the enum: the Subscription.Status setting spells "Trialling", which never matches
const globalFilters = computed(() => [
  {
    id: "status",
    label: t("SUBSCRIPTIONS.FIELDS.STATUS"),
    filter: { options: Object.values(SubscriptionStatus).map((s) => ({ value: s, label: s })) },
  },
  { id: "created", label: t("SUBSCRIPTIONS.FIELDS.CREATED"), filter: { range: ["startDate", "endDate"] as [string, string] } },
]);

function onFilter(event: { filters: Record<string, unknown> }) {
  const status = event.filters.status as string | string[] | undefined;
  filters.value = {
    statuses: status ? ([] as string[]).concat(status) : undefined,
    startDate: event.filters.startDate ? new Date(event.filters.startDate as string) : undefined,
    endDate: event.filters.endDate ? new Date(event.filters.endDate as string) : undefined,
  };
  // a page change loads through the watcher below
  if (pagination.skip) pagination.setPage(1);
  else load();
}

load();
watch(searchValue, () => pagination.setPage(1));
watch([sortExpression, searchValue, () => pagination.skip], debounce(load, 300));

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SUBSCRIPTIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "delete",
    title: t("SUBSCRIPTIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "subscription:delete",
    disabled: computed(() => !selection.value.length),
    async clickHandler() {
      if (await showConfirmation(t("SUBSCRIPTIONS.ALERTS.DELETE_SELECTED", { count: selection.value.length }))) {
        // as the AngularJS list: a deleted subscription's blade, saved, would re-create it
        await closeChildren();
        await (await getApiClient()).deleteSubscriptionsByIds(selection.value.map((x) => x.id!));
        selection.value = [];
        await load();
      }
    },
  },
]);

function openDetails(id: string) {
  openBlade({ name: "SubscriptionDetails", param: id });
}

exposeToChildren({ reload: () => load() });
</script>

<style lang="scss">
.subscriptions-list__number {
  color: var(--neutrals-900);
}
</style>
