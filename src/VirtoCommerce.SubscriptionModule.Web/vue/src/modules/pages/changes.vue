<template>
  <VcBlade
    :title="$t('SUBSCRIPTIONS.WIDGETS.CHANGES')"
    :loading="loading"
    width="50%"
  >
    <!-- the platform change log of the subscription -->
    <VcDataTable
      :items="items"
      :state-key="'SUBSCRIPTION_CHANGES'"
      :empty-state="{ icon: 'lucide-history', title: $t('SUBSCRIPTIONS.PAGES.CHANGES.EMPTY') }"
    >
      <VcColumn
        id="createdDate"
        :title="$t('SUBSCRIPTIONS.PAGES.CHANGES.DATE')"
        type="date-ago"
        always-visible
      />
      <VcColumn
        id="operationType"
        :title="$t('SUBSCRIPTIONS.PAGES.CHANGES.TYPE')"
      />
      <VcColumn
        id="createdBy"
        :title="$t('SUBSCRIPTIONS.PAGES.CHANGES.USER')"
      />
      <VcColumn
        id="detail"
        :title="$t('SUBSCRIPTIONS.PAGES.CHANGES.DETAIL')"
        always-visible
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { ChangeLogClient, useApiClient, useAsync, useBlade, type OperationLog } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { onMounted, ref } from "vue";

defineBlade({
  url: "/subscription-changes",
  name: "SubscriptionChanges",
});

const { param } = useBlade();
const { getApiClient } = useApiClient(ChangeLogClient);
const items = ref<OperationLog[]>([]);

const { loading, action: load } = useAsync(async () => {
  items.value = await (await getApiClient()).searchChanges({ objectIds: [param.value!], objectType: "Subscription", take: 100 });
});

onMounted(load);
</script>
