<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';

const store = useStore();
const toast = useToast();

const scrollerRef = ref(null);
let resizeObserver;

const statuses = computed(() => store.state.statuses.items);
const selectedStatusId = computed(() => store.state.statuses.selectedStatusId);
const isLoading = computed(() => store.state.statuses.isLoading && !store.state.statuses.hasLoaded);

const chipRefs = reactive({});
const layout = reactive({
  pages: [],
  activePage: 0,
});

const hasOverflow = computed(() => layout.pages.length > 1);
const canScrollLeft = computed(() => hasOverflow.value && layout.activePage > 0);
const canScrollRight = computed(
  () => hasOverflow.value && layout.activePage < layout.pages.length - 1,
);

const registerChip = (el, id) => {
  if (el) {
    chipRefs[id] = el;
  } else {
    delete chipRefs[id];
  }
};

const ensureStatusesLoaded = async () => {
  if (!store.state.statuses.hasLoaded && !store.state.statuses.isLoading) {
    try {
      await store.dispatch('statuses/fetchAll');
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Не удалось загрузить статусы',
        detail: error.message ?? 'Попробуйте обновить страницу.',
        life: 5000,
      });
    }
  }
};

const selectStatus = (statusId) => {
  if (selectedStatusId.value === statusId) return;
  store.dispatch('statuses/select', statusId);
};

const statusStyle = (status) => {
  const isActive = status.id === selectedStatusId.value;
  if (!isActive) {
    return {
      backgroundColor: status.color,
      color: status.textColor,
      borderColor: status.color,
    };
  }

  return {
    '--selected-bg': '#ffffff',
    '--selected-color': status.color,
    backgroundColor: '#ffffff',
    color: status.color,
    borderColor: status.color,
  };
};
const getChipElements = () =>
  statuses.value.map((status) => chipRefs[status.id]).filter((chip) => Boolean(chip));

const ensureSelectedVisible = () => {
  if (!selectedStatusId.value || !layout.pages.length) return;
  const targetIndex = statuses.value.findIndex(
    (status) => status.id === selectedStatusId.value,
  );
  if (targetIndex === -1) return;
  const pageIndex = layout.pages.findIndex(
    ({ start, end }) => targetIndex >= start && targetIndex <= end,
  );
  if (pageIndex !== -1 && pageIndex !== layout.activePage) {
    layout.activePage = pageIndex;
  }
};

const recalculatePages = () => {
  nextTick(() => {
    const container = scrollerRef.value;
    if (!container) {
      layout.pages = [];
      layout.activePage = 0;
      return;
    }
    const chips = getChipElements();
    if (!chips.length) {
      layout.pages = [];
      layout.activePage = 0;
      return;
    }

    const containerWidth = container.clientWidth;
    if (!containerWidth) return;

    const pages = [];
    let pageStartIndex = 0;
    let pageStartLeft = chips[0].offsetLeft;

    chips.forEach((chip, index) => {
      const chipRight = chip.offsetLeft + chip.offsetWidth;
      if (chipRight - pageStartLeft > containerWidth && index !== pageStartIndex) {
        pages.push({
          start: pageStartIndex,
          end: index - 1,
          offset: pageStartLeft,
        });
        pageStartIndex = index;
        pageStartLeft = chip.offsetLeft;
      }
    });

    pages.push({
      start: pageStartIndex,
      end: chips.length - 1,
      offset: chips[pageStartIndex].offsetLeft,
    });

    const nextActive = Math.min(layout.activePage, pages.length - 1);
    layout.pages = pages;
    layout.activePage = nextActive < 0 ? 0 : nextActive;
    ensureSelectedVisible();
  });
};

const goToPage = (direction) => {
  if (!layout.pages.length) return;
  const nextIndex = Math.min(
    Math.max(layout.activePage + direction, 0),
    layout.pages.length - 1,
  );
  layout.activePage = nextIndex;
};

const trackTransform = computed(() => {
  if (!layout.pages.length) return {};
  const firstStatus = statuses.value[0];
  if (!firstStatus) return {};
  const firstChip = chipRefs[firstStatus.id];
  if (!firstChip) return {};
  const page = layout.pages[layout.activePage];
  if (!page) return {};
  const shift = Math.max(page.offset - firstChip.offsetLeft, 0);
  return {
    transform: `translateX(-${shift}px)`,
  };
});

watch(statuses, () => {
  recalculatePages();
});

watch(selectedStatusId, () => {
  ensureSelectedVisible();
});

const initResizeObserver = () => {
  if (resizeObserver) {
    resizeObserver.disconnect();
    resizeObserver = undefined;
  }
  if (!window.ResizeObserver || !scrollerRef.value) return;
  resizeObserver = new ResizeObserver(() => recalculatePages());
  resizeObserver.observe(scrollerRef.value);
};

onMounted(async () => {
  await ensureStatusesLoaded();
  nextTick(() => {
    initResizeObserver();
    recalculatePages();
  });
});

onBeforeUnmount(() => {
  if (resizeObserver) {
    resizeObserver.disconnect();
    resizeObserver = undefined;
  }
});
</script>

<template>
  <section :class="['status-picker w-100 position-relative', { 'has-overflow': hasOverflow }]">
    <button
      v-if="hasOverflow"
      class="nav-btn left border-0 rounded-circle"
      type="button"
      :disabled="!canScrollLeft"
      aria-label="Прокрутить статусы влево"
      @click="goToPage(-1)"
    >
      <i class="pi pi-chevron-left"></i>
    </button>

    <div class="status-track flex-grow-1" ref="scrollerRef">
      <div class="status-strip d-flex align-items-center gap-2" :style="trackTransform">
        <div v-if="isLoading" class="text-muted small">Загружаем статусы…</div>
        <div v-else-if="!statuses.length" class="text-muted small">Статусы ещё не созданы</div>
        <button
          v-for="status in statuses"
          :key="status.id"
          :ref="(el) => registerChip(el, status.id)"
          type="button"
          class="status-chip border rounded-pill px-4 py-2 d-flex align-items-center gap-2"
          :class="{ active: status.id === selectedStatusId }"
          :style="statusStyle(status)"
          @click="selectStatus(status.id)"
        >
          <span class="fw-semibold">{{ status.name }}</span>
        </button>
      </div>
    </div>

    <button
      v-if="hasOverflow"
      class="nav-btn right border-0 rounded-circle"
      type="button"
      :disabled="!canScrollRight"
      aria-label="Прокрутить статусы вправо"
      @click="goToPage(1)"
    >
      <i class="pi pi-chevron-right"></i>
    </button>
  </section>
</template>

<style scoped>
.status-picker {
  position: relative;
  flex: 1 1 auto;
  min-width: 0;
  min-height: 56px;
  display: flex;
  align-items: center;
}

.status-picker.has-overflow {
  display: grid;
  grid-template-columns: auto 1fr auto;
  column-gap: 0;
}

.status-track {
  overflow: hidden;
  width: 100%;
  min-width: 0;
  flex: 1 1 auto;
}

.status-picker.has-overflow .status-track {
  grid-column: 2;
}

.status-strip {
  min-height: 48px;
  width: max-content;
  transition: transform 0.35s ease;
}

.status-chip {
  border: 1px solid transparent;
  background-color: rgba(15, 23, 42, 0.05);
  color: #0f172a;
  transition: transform 0.15s ease, box-shadow 0.15s ease, opacity 0.15s ease, border-color 0.15s ease;
  white-space: nowrap;
}

.status-chip.active {
  transform: translateY(-2px);
  filter: brightness(1);
}

.status-chip:hover {
  opacity: 0.9;
}

.status-chip:focus,
.status-chip:focus-visible {
  outline: none;
}

.nav-btn {
  width: 34px;
  height: 34px;
  background-color: rgba(15, 23, 42, 0.08);
  color: #0f172a;
  transition: background-color 0.2s ease, color 0.2s ease;
  margin: 0 0.25rem;
}

.nav-btn:disabled {
  opacity: 0.35;
}

.nav-btn:not(:disabled):hover {
  background-color: rgba(15, 23, 42, 0.15);
}
</style>

