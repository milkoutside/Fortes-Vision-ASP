<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import { useStore } from 'vuex';

const store = useStore();

const emit = defineEmits(['select']);

const calendarHeader = ref(null);

// Данные из store
const threeMonthsData = computed(() => store.getters['calendar/threeMonthsData']);
const isWeekend = computed(() => store.getters['calendar/isWeekend']);
const isToday = computed(() => store.getters['calendar/isToday']);
const getWeekday = computed(() => store.getters['calendar/getWeekday']);
const getDayNumber = computed(() => store.getters['calendar/getDayNumber']);

const selectDate = (dateStr) => {
  emit('select', dateStr);
};

const EDGE_PROXIMITY_PX = 40;
const EDGE_HOLD_MS = 500;
const INACTIVITY_RESET_MS = 350;

const isAutoNavigating = ref(false);
const isProgrammaticScroll = ref(false);
const isPointerDown = ref(false);
const isDragging = ref(false);
const isWheelActive = ref(false);

let dragStartX = 0;
let dragStartScrollLeft = 0;
let activePointerId = null;

const userActive = computed(() => isPointerDown.value || isWheelActive.value);

const edgeHoldSide = ref(null);
let edgeHoldStart = 0;
let edgeHoldRaf = 0;
let wheelActivityTimeout = 0;

const leftProgress = ref(0);
const rightProgress = ref(0);

const clamp01 = (v) => Math.max(0, Math.min(1, v));
const waitForFrame = () => new Promise((resolve) => requestAnimationFrame(() => resolve()));

const getHeaderEl = () => calendarHeader.value;

const nearLeftEdge = (el) => (el ? el.scrollLeft <= EDGE_PROXIMITY_PX : false);

const nearRightEdge = (el) => {
  if (!el) return false;
  const maxScrollLeft = el.scrollWidth - el.clientWidth;
  return maxScrollLeft - el.scrollLeft <= EDGE_PROXIMITY_PX;
};

const resetProgress = () => {
  leftProgress.value = 0;
  rightProgress.value = 0;
};

const cancelEdgeHold = () => {
  edgeHoldSide.value = null;
  if (edgeHoldRaf) cancelAnimationFrame(edgeHoldRaf);
  edgeHoldRaf = 0;
  resetProgress();
};

const tickEdgeHold = (ts) => {
  if (!edgeHoldSide.value) return;
  const elapsed = ts - edgeHoldStart;
  const progress = clamp01(elapsed / EDGE_HOLD_MS);
  if (edgeHoldSide.value === 'left') {
    leftProgress.value = progress;
  } else {
    rightProgress.value = progress;
  }

  const header = getHeaderEl();
  const stillNear = edgeHoldSide.value === 'left' ? nearLeftEdge(header) : nearRightEdge(header);

  if (!stillNear || !userActive.value || isAutoNavigating.value || isProgrammaticScroll.value) {
    cancelEdgeHold();
    return;
  }

  if (progress >= 1) {
    const side = edgeHoldSide.value;
    cancelEdgeHold();
    if (side === 'left') {
      void goToPreviousThreeMonths(true);
    } else {
      void goToNextThreeMonths(true);
    }
    return;
  }

  edgeHoldRaf = requestAnimationFrame(tickEdgeHold);
};

const startEdgeHold = (side) => {
  if (isAutoNavigating.value || isProgrammaticScroll.value) return;
  const header = getHeaderEl();
  if (!header) return;

  if (edgeHoldSide.value !== side) {
    edgeHoldStart = performance.now();
    if (side === 'left') {
      leftProgress.value = 0;
    } else {
      rightProgress.value = 0;
    }
  }

  edgeHoldSide.value = side;
  if (!edgeHoldRaf) edgeHoldRaf = requestAnimationFrame(tickEdgeHold);
};

const goToPreviousThreeMonths = async (fromAuto = false) => {
  if (isAutoNavigating.value) return;
  isAutoNavigating.value = !!fromAuto;
  store.dispatch('calendar/goToPreviousThreeMonths');
  await nextTick();
  resetScrollToStart();
  setTimeout(() => {
    isAutoNavigating.value = false;
  }, 400);
};

const goToNextThreeMonths = async (fromAuto = false) => {
  if (isAutoNavigating.value) return;
  isAutoNavigating.value = !!fromAuto;
  store.dispatch('calendar/goToNextThreeMonths');
  await nextTick();
  resetScrollToStart();
  setTimeout(() => {
    isAutoNavigating.value = false;
  }, 400);
};

const ensureTodayInRange = async () => {
  const today = store.getters['calendar/today'];
  const allDates = threeMonthsData.value.flatMap((month) => month.dates);
  if (allDates.includes(today)) return;

  isAutoNavigating.value = true;
  try {
    await store.dispatch('calendar/goToToday');
    await nextTick();
    await waitForFrame();
  } finally {
    isAutoNavigating.value = false;
  }
};

const findTodayCell = async () => {
  const header = getHeaderEl();
  if (!header) return null;

  for (let attempt = 0; attempt < 4; attempt += 1) {
    const cell = header.querySelector('.date-cell.today');
    if (cell) return cell;
    await nextTick();
    await waitForFrame();
  }

  return null;
};

const scrollToToday = async () => {
  await ensureTodayInRange();

  const header = getHeaderEl();
  if (!header) return;

  const finalCell = await findTodayCell();
  if (!finalCell) return;

  await waitForFrame();

  const headerRect = header.getBoundingClientRect();
  const cellRect = finalCell.getBoundingClientRect();
  const cellLeftWithinHeader = cellRect.left - headerRect.left + header.scrollLeft;
  const headerWidth = header.clientWidth;
  const cellWidth = finalCell.offsetWidth;
  const targetScrollLeft = Math.max(0, cellLeftWithinHeader - (headerWidth - cellWidth) / 2);

  isProgrammaticScroll.value = true;
  const calendarCells = document.querySelector('.images-virtual-container');
  header.scrollTo({
    left: targetScrollLeft,
    behavior: 'smooth',
  });

  if (calendarCells) {
    calendarCells.scrollTo({
      left: targetScrollLeft,
      behavior: 'smooth',
    });
  }

  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      isProgrammaticScroll.value = false;
    });
  });

  setTimeout(() => {
    isProgrammaticScroll.value = false;
  }, 400);
};

const resetScrollToStart = () => {
  const header = getHeaderEl();
  if (!header) return;

  isProgrammaticScroll.value = true;
  header.scrollTo({
    left: 0,
    behavior: 'smooth',
  });

  setTimeout(() => {
    isProgrammaticScroll.value = false;
  }, 400);

  const calendarCells = document.querySelector('.images-virtual-container');
  if (calendarCells) {
    calendarCells.scrollLeft = 0;
  }
};

const handleCalendarScroll = (event) => {
  const header = event.target;
  const targetScrollLeft = header.scrollLeft;
  const calendarCells = document.querySelector('.images-virtual-container');
  if (calendarCells) {
    calendarCells.scrollLeft = targetScrollLeft;
  }

  if (isProgrammaticScroll.value || isAutoNavigating.value) return;

  const atLeft = nearLeftEdge(header);
  const atRight = nearRightEdge(header);

  if (userActive.value && (atLeft || atRight)) {
    startEdgeHold(atLeft ? 'left' : 'right');
  } else {
    cancelEdgeHold();
  }
};

const shouldIgnorePointer = (event) => {
  const interactiveTarget = event.target instanceof Element && event.target.closest('.nav-btn');
  return !!interactiveTarget;
};

const onPointerDown = (e) => {
  if (shouldIgnorePointer(e) || e.button !== 0) return;
  isPointerDown.value = true;
  const header = getHeaderEl();
  if (!header) return;

  isDragging.value = true;
  activePointerId = e.pointerId;
  header.setPointerCapture && header.setPointerCapture(e.pointerId);
  dragStartX = e.clientX;
  dragStartScrollLeft = header.scrollLeft;
};

const onPointerMove = (e) => {
  if (!isDragging.value || e.pointerId !== activePointerId) return;
  const header = getHeaderEl();
  if (!header) return;

  const deltaX = e.clientX - dragStartX;
  header.scrollLeft = dragStartScrollLeft - deltaX;
};

const onPointerUp = (e) => {
  isPointerDown.value = false;
  const header = getHeaderEl();
  if (header && e && e.pointerId === activePointerId) {
    header.releasePointerCapture && header.releasePointerCapture(e.pointerId);
  }

  isDragging.value = false;
  activePointerId = null;
  cancelEdgeHold();
};

const onWheel = (e) => {
  const header = getHeaderEl();
  if (!header) return;

  const primaryDelta = Math.abs(e.deltaX) > Math.abs(e.deltaY) ? e.deltaX : e.deltaY;
  if (primaryDelta !== 0) {
    e.preventDefault();
    header.scrollLeft += primaryDelta;
  }

  isWheelActive.value = true;
  clearTimeout(wheelActivityTimeout);
  wheelActivityTimeout = setTimeout(() => {
    isWheelActive.value = false;
  }, INACTIVITY_RESET_MS);
};

const syncCalendarScroll = () => {
  const header = getHeaderEl();
  const calendarCells = document.querySelector('.images-virtual-container');
  if (header && calendarCells) {
    calendarCells.scrollLeft = header.scrollLeft;
  }
};

onMounted(() => {
  const header = getHeaderEl();
  if (header) {
    header.addEventListener('scroll', handleCalendarScroll, { passive: true });
    header.addEventListener('wheel', onWheel, { passive: false });
  }

  nextTick(() => {
    scrollToToday();
  });
});

onUnmounted(() => {
  const header = getHeaderEl();
  if (header) {
    header.removeEventListener('scroll', handleCalendarScroll);
    header.removeEventListener('wheel', onWheel);
  }
  cancelEdgeHold();
});

const edgeGlowStyles = computed(() => ({
  '--edge-left': leftProgress.value,
  '--edge-right': rightProgress.value,
}));

defineExpose({
  syncCalendarScroll,
  scrollToToday,
});
</script>

<template>
  <div class="calendar-container">
    <div
      ref="calendarHeader"
      id="calendar-dates-scroll"
      class="calendar-header"
      :style="edgeGlowStyles"
      :class="{ dragging: isDragging }"
      @pointerdown="onPointerDown"
      @pointermove="onPointerMove"
      @pointerup="onPointerUp"
      @pointercancel="onPointerUp"
      @pointerleave="onPointerUp"
    >
      <div class="dates-container">
        <div
          v-for="(month, monthIndex) in threeMonthsData"
          :key="`${month.year}-${month.month}`"
          class="month-section"
        >
          <div v-if="monthIndex === 0" class="edge-glow left"></div>
          <div v-if="monthIndex === threeMonthsData.length - 1" class="edge-glow right"></div>

          <div class="month-header">
            <button
              v-if="monthIndex === 0"
              @click.stop="goToPreviousThreeMonths"
              class="nav-btn nav-btn-left"
              type="button"
            >
              ‹‹
            </button>
            <div v-else class="nav-placeholder"></div>

            <span class="month-title">{{ month.monthName }} {{ month.year }}</span>

            <button
              v-if="monthIndex === threeMonthsData.length - 1"
              @click.stop="goToNextThreeMonths"
              class="nav-btn nav-btn-right"
              type="button"
            >
              ››
            </button>
            <div v-else class="nav-placeholder"></div>
          </div>

          <div class="dates-row">
            <div
              v-for="date in month.dates"
              :key="date"
              class="date-cell"
              :class="{
                weekend: isWeekend(date),
                today: isToday(date),
                'current-month': monthIndex === 1
              }"
              :data-date="date"
              @click="selectDate(date)"
            >
              <div class="day">{{ getDayNumber(date) }}</div>
              <div class="weekday">{{ getWeekday(date) }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.calendar-container {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: white;
  height: var(--header-height, clamp(120px, 15vh, 180px));
  min-height: var(--header-height, clamp(120px, 15vh, 180px));
  position: sticky;
  top: 0;
  z-index: 10;
  box-sizing: border-box;
  flex-shrink: 0;
  border-bottom: 1px solid #eee;
  width: 100%;
  overflow: hidden;
}

.calendar-header {
  background: white;
  overflow-x: auto;
  overflow-y: hidden;
  scrollbar-width: thin;
  scroll-behavior: smooth;
  height: 100%;
  box-sizing: border-box;
  position: relative;
  --edge-left: 0;
  --edge-right: 0;
}

.calendar-header::-webkit-scrollbar {
  height: 2px;
}

.calendar-header::-webkit-scrollbar-track {
  background: transparent;
}

.calendar-header::-webkit-scrollbar-thumb {
  background-color: #bbb;
  border-radius: 1px;
}

.calendar-header::-webkit-scrollbar-thumb:hover {
  background-color: #999;
}

.dates-container {
  display: flex;
  width: fit-content;
  height: 100%;
  position: relative;
}

.month-section {
  display: flex;
  flex-direction: column;
  min-width: fit-content;
  border-right: 2px solid #ddd;
  border-bottom: 1px solid #ddd;
  height: 100%;
  position: relative;
  background: white;
  justify-content: flex-start;
}

.month-section:last-child {
  border-right: none;
}

.month-header {
  font-size: 12px;
  font-weight: 600;
  color: #666;
  text-align: center;
  padding: 4px 12px;
  background: #f8f9fa;
  border-bottom: 1px solid #eee;
  white-space: nowrap;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex: 1;
  box-sizing: border-box;
}

.month-title {
  flex: 1;
  text-align: center;
  margin: 0 8px;
  font-weight: 900;
  font-size: 1.2rem;
}

.nav-btn {
  width: 32px;
  height: 32px;
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 50%;
  font-size: 16px;
  font-weight: bold;
  color: #666;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
  pointer-events: auto;
  z-index: 10;
  position: relative;
}

.nav-btn:hover {
  background: #e9ecef;
  color: #333;
}

.nav-placeholder {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
}

.dates-row {
  display: flex;
  flex: 0 0 6vh;
  min-width: max-content;
  height: 6vh;
  flex-shrink: 0;
}

.date-cell {
  width: 4vw;
  min-width: 4vw;
  max-width: 4vw;
  height: 100%;
  border-right: 1px solid #eee;
  text-align: center;
  padding: 2px;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  flex-shrink: 0;
  box-sizing: border-box;
}

.date-cell:hover {
  background: #f0f0f0;
}

.date-cell.weekend {
  background: #fafafa;
  color: #e74c3c;
  border-right: 1px solid #eee;
}

.date-cell.today {
  background: #3498db;
  color: white;
}

.date-cell.today:hover {
  background: #2980b9;
}

.date-cell.current-month {
  background: #f8f9fa;
}

.date-cell.current-month.weekend {
  background: #f0f0f0;
  border-right: 1px solid #eee;
}

.date-cell.current-month.today {
  background: #3498db;
}

.day {
  font-size: 12px;
  font-weight: 500;
  line-height: 1;
}

.weekday {
  font-size: 9px;
  color: #666;
  line-height: 1;
  margin-top: 1px;
}

.date-cell.today .weekday {
  color: rgba(255, 255, 255, 0.8);
}

.date-cell.weekend .weekday {
  color: #c0392b;
}

.date-cell.current-month .weekday {
  color: #555;
}

.edge-glow {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 88px;
  pointer-events: none;
  opacity: 0;
  transition: opacity 100ms linear;
  z-index: 2;
}

.edge-glow.left {
  left: 0;
  background: linear-gradient(90deg, rgba(52, 152, 219, 0.55) 0%, rgba(52, 152, 219, 0.28) 50%, rgba(52, 152, 219, 0) 100%);
  opacity: var(--edge-left);
}

.edge-glow.right {
  right: 0;
  background: linear-gradient(270deg, rgba(52, 152, 219, 0.55) 0%, rgba(52, 152, 219, 0.28) 50%, rgba(52, 152, 219, 0) 100%);
  opacity: var(--edge-right);
}

.calendar-header.dragging {
  cursor: grabbing;
}

.calendar-header {
  cursor: grab;
}
</style>
