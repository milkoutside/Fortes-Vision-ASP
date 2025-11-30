<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref } from 'vue';

const props = defineProps({
  rows: {
    type: Array,
    default: () => [],
  },
  dates: {
    type: Array,
    default: () => [],
  },
  blockWidth: {
    type: String,
    default: '0px',
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const containerRef = ref(null);
let headerEl = null;
const ROW_HEIGHT = 80;
const ROW_PADDING = 18;
const LANE_GAP = 8;

const dateIndexMap = computed(() => {
  const map = new Map();
  props.dates.forEach((date, index) => {
    map.set(date, index);
  });
  return map;
});

const processedRows = computed(() => {
  return (props.rows ?? []).map((row) => {
    const { segments, laneCount } = processRowSegments(row);
    return {
      ...row,
      _segments: segments,
      _laneCount: laneCount,
    };
  });
});

const processRowSegments = (row) => {
  const rawSegments = Array.isArray(row.segments) ? row.segments : [];
  if (row.type !== 'user') {
    return {
      segments: rawSegments.map((segment) => ({ ...segment, laneIndex: 0 })),
      laneCount: 1,
    };
  }

  const lanes = [];
  const segments = rawSegments.map((segment) => {
    const startIndex = clampIndex(segment.startDate);
    const endIndex = clampIndex(segment.endDate);
    const laneIndex = findAvailableLane(lanes, startIndex, endIndex);
    lanes[laneIndex].push({ startIndex, endIndex });
    return { ...segment, laneIndex };
  });
  return {
    segments,
    laneCount: Math.max(1, lanes.length),
  };
};

const findAvailableLane = (lanes, startIndex, endIndex) => {
  let laneIndex = 0;
  while (true) {
    if (!lanes[laneIndex]) {
      lanes[laneIndex] = [];
      break;
    }
    const overlaps = lanes[laneIndex].some(
      (laneRange) =>
        !(endIndex < laneRange.startIndex - 0.01 || startIndex > laneRange.endIndex + 0.01),
    );
    if (!overlaps) {
      break;
    }
    laneIndex += 1;
  }
  if (!lanes[laneIndex]) {
    lanes[laneIndex] = [];
  }
  return laneIndex;
};

const clampIndex = (dateStr) => {
  if (!props.dates.length) {
    return 0;
  }
  if (dateIndexMap.value.has(dateStr)) {
    return dateIndexMap.value.get(dateStr);
  }
  const fallbackIndex = props.dates.findIndex((value) => value >= dateStr);
  if (fallbackIndex === -1) {
    return props.dates.length - 1;
  }
  return Math.max(0, fallbackIndex);
};

const getRowStyle = () => ({
  height: `${ROW_HEIGHT}px`,
});

const getTrackStyle = () => ({
  height: `${ROW_HEIGHT}px`,
});

const getLaneMetrics = (row) => {
  const laneCount = Math.max(1, row._laneCount ?? 1);
  const available = ROW_HEIGHT - ROW_PADDING * 2 - LANE_GAP * (laneCount - 1);
  const laneHeight = Math.max(6, available / laneCount);
  return { laneCount, laneHeight };
};

const getBarStyle = (row, segment) => {
  const startIndex = clampIndex(segment.startDate);
  const endIndex = clampIndex(segment.endDate);
  const span = Math.max(1, endIndex - startIndex + 1);
  const laneIndex = segment.laneIndex ?? 0;
  const { laneHeight } = getLaneMetrics(row);
  const top = ROW_PADDING + laneIndex * (laneHeight + LANE_GAP);
  return {
    left: `calc(var(--day-width) * ${startIndex})`,
    width: `calc(var(--day-width) * ${span})`,
    backgroundColor: segment.color ?? '#2563eb',
    color: segment.textColor ?? '#ffffff',
    top: `${top}px`,
    height: `${laneHeight}px`,
  };
};

const getBarTooltip = (row, segment) => {
  if (segment.tooltip) return segment.tooltip;
  const start = segment.startDate ?? '';
  const end = segment.endDate ?? '';
  if (row.type === 'task') {
    return `${row.label}: ${start} — ${end}`;
  }
  return `${segment.projectName ?? row.label}: ${start} — ${end}`;
};

const handleContainerScroll = (event) => {
  if (headerEl) {
    headerEl.scrollLeft = event.target.scrollLeft;
  }
};

const syncWithHeader = () => {
  if (headerEl && containerRef.value) {
    containerRef.value.scrollLeft = headerEl.scrollLeft;
  }
};

onMounted(() => {
  headerEl = document.getElementById('calendar-dates-scroll');
  if (headerEl) {
    headerEl.addEventListener('scroll', syncWithHeader);
  }
  if (containerRef.value) {
    containerRef.value.addEventListener('scroll', handleContainerScroll);
  }
  nextTick(() => {
    syncWithHeader();
  });
});

onUnmounted(() => {
  if (headerEl) {
    headerEl.removeEventListener('scroll', syncWithHeader);
  }
  if (containerRef.value) {
    containerRef.value.removeEventListener('scroll', handleContainerScroll);
  }
});

defineExpose({
  getScrollElement: () => containerRef.value,
});
</script>

<template>
  <div class="timeline-shell">
    <div
      ref="containerRef"
      class="workload-timeline images-virtual-container"
    >
      <div class="timeline-content" :style="{ width: blockWidth }">
        <div
          v-for="row in processedRows"
          :key="row.id"
          class="timeline-row"
          :class="[`row-${row.type}`]"
          :style="getRowStyle(row)"
        >
          <div class="timeline-track" :style="getTrackStyle(row)">
            <div
              v-for="(segment, index) in row._segments ?? []"
              :key="`${row.id}-${segment.startDate}-${segment.endDate}-${index}`"
              class="timeline-bar"
              :class="[`bar-${row.type}`]"
              :style="getBarStyle(row, segment)"
              :title="getBarTooltip(row, segment)"
            >
              <span v-if="row.type === 'task'" class="bar-label">
                {{ segment.label ?? row.meta?.taskLabel ?? row.label }}
              </span>
              <span v-else-if="row.type === 'project'" class="bar-label bar-label-project">
                {{ segment.projectName ?? row.label }}
              </span>
              <span v-else-if="row.type === 'user'" class="bar-label bar-label-user">
                {{ segment.projectName ?? '' }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="loading" class="timeline-overlay">
      <span class="spinner"></span>
      <span>Загружаем свежие данные…</span>
    </div>
  </div>
</template>

<style scoped>
.timeline-shell {
  position: relative;
  flex: 1;
  min-height: 0;
}

.workload-timeline {
  --day-width: 4vw;
  width: 100%;
  height: 100%;
  overflow: auto;
  position: relative;
  background: #ffffff;
}

.timeline-content {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.timeline-row {
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  height: 80px;
  transition: background-color 0.15s ease;
  background-color: #ffffff;
}

.timeline-row:hover {
  background-color: #f8fafc;
}

.row-project {
  background-color: #fafbfc;
}

.row-task {
  background-color: #fcfcfd;
}

.timeline-track {
  position: relative;
  width: 100%;
  height: 80px;
}

.timeline-bar {
  position: absolute;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: flex-start;
  font-size: 0.85rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  padding: 0 0.8rem;
  box-shadow: 0 2px 4px rgba(15, 23, 42, 0.12);
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  cursor: pointer;
}

.timeline-bar:hover {
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.15);
  transform: translateY(-2px);
  z-index: 5;
}

.bar-user {
  height: 24px;
  box-shadow: 0 1px 3px rgba(15, 23, 42, 0.1);
  opacity: 0.92;
  padding: 0 0.7rem;
  border-radius: 8px;
}

.bar-user:hover {
  opacity: 1;
  box-shadow: 0 3px 8px rgba(15, 23, 42, 0.18);
  transform: translateY(-1px);
}

.bar-project {
  height: 28px;
  opacity: 0.95;
  font-size: 0.8rem;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.bar-task {
  height: 32px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  font-weight: 600;
}

.bar-label {
  font-weight: 600;
  font-size: 0.8rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.bar-label-project {
  font-size: 0.75rem;
  font-weight: 600;
}

.bar-label-user {
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.01em;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.15);
}

.timeline-overlay {
  position: absolute;
  inset: 0;
  background: rgba(248, 250, 252, 0.8);
  backdrop-filter: blur(2px);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  font-weight: 600;
  color: #1e293b;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 3px solid rgba(148, 163, 184, 0.5);
  border-top-color: #0ea5e9;
  border-radius: 50%;
  animation: spin 0.9s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

