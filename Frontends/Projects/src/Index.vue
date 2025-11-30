<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import { useStore } from 'vuex';
import AppHeader from './components/AppHeader.vue';
import SettingsModal from './components/settings/SettingsModal.vue';
import CreateProjectModal from './components/projects/CreateProjectModal.vue';
import ProjectsList from './components/projects/ProjectsList.vue';
import CalendarHeader from './components/calendar/CalendarHeader.vue';
import CalendarCells from './components/calendar/CalendarCells.vue';

const store = useStore();
const isSettingsOpen = ref(false);
const isCreateProjectOpen = ref(false);

// Refs для компонентов
const projectsListRef = ref(null);
const calendarCellsRef = ref(null);
const calendarHeaderRef = ref(null);

// Данные из store
const projects = computed(() => store.state.projects.items);

// Данные из ProjectsList (реактивно обновляются через computed)
const expandedProjects = computed(() => projectsListRef.value?.expandedProjects ?? new Set());
const batchesByProject = computed(() => projectsListRef.value?.batchesByProject ?? new Map());
const expandedBatches = computed(() => projectsListRef.value?.expandedBatches ?? new Set());
const imagesByBatch = computed(() => projectsListRef.value?.imagesByBatch ?? new Map());
const loadingBatches = computed(() => projectsListRef.value?.loadingBatches ?? new Set());
const loadingImages = computed(() => projectsListRef.value?.loadingImages ?? new Set());

// Синхронизация вертикального скролла (только от списка проектов к ячейкам)
const syncVerticalScroll = (event) => {
  const projectsContainer = projectsListRef.value?.listContainer;
  const cellsScrollWrapper = calendarCellsRef.value?.cellsScrollWrapper;
  
  if (!projectsContainer || !cellsScrollWrapper) return;
  
  // Синхронизируем скролл ячеек с проектами
  cellsScrollWrapper.scrollTop = projectsContainer.scrollTop;
};

const setupScrollSync = () => {
  nextTick(() => {
    const projectsContainer = projectsListRef.value?.listContainer;
    
    if (projectsContainer) {
      projectsContainer.addEventListener('scroll', syncVerticalScroll, { passive: true });
    }
    
    // Синхронизируем горизонтальный скролл календаря после монтирования
    if (calendarCellsRef.value?.syncScroll) {
      calendarCellsRef.value.syncScroll();
    }
  });
};

const cleanupScrollSync = () => {
  const projectsContainer = projectsListRef.value?.listContainer;
  
  if (projectsContainer) {
    projectsContainer.removeEventListener('scroll', syncVerticalScroll);
  }
};

onMounted(() => {
  // Даем время компонентам инициализироваться
  setTimeout(setupScrollSync, 100);
  
  // Дополнительная синхронизация после завершения плавной прокрутки
  setTimeout(() => {
    if (calendarCellsRef.value?.syncScroll) {
      calendarCellsRef.value.syncScroll();
    }
  }, 600);
});

onUnmounted(() => {
  cleanupScrollSync();
});
</script>

<template>
  <div class="app-shell min-vh-100 d-flex flex-column bg-body-tertiary">
    <Toast />
    <ConfirmDialog />

    <AppHeader
      @open-settings="isSettingsOpen = true"
      @open-create-project="isCreateProjectOpen = true"
    />
    <main class="flex-grow-1 d-flex">
      <!-- Левая часть: список проектов -->
      <div class="projects-panel" style="width: 400px; min-width: 300px; max-width: 500px;">
        <ProjectsList ref="projectsListRef" />
      </div>
      
      <!-- Правая часть: календарь + ячейки -->
      <div class="content-panel flex-grow-1 d-flex flex-column">
        <!-- Верхняя часть: заголовок календаря -->
        <CalendarHeader ref="calendarHeaderRef" />
        
        <!-- Нижняя часть: ячейки календаря -->
        <CalendarCells
          ref="calendarCellsRef"
          :projects="projects"
          :expanded-projects="expandedProjects"
          :batches-by-project="batchesByProject"
          :expanded-batches="expandedBatches"
          :images-by-batch="imagesByBatch"
          :loading-batches="loadingBatches"
          :loading-images="loadingImages"
          :is-loading-more="store.state.projects.isLoadingMore"
          :has-more="store.state.projects.hasMore"
        />
      </div>
    </main>

    <SettingsModal v-model:visible="isSettingsOpen" />
    <CreateProjectModal v-model:visible="isCreateProjectOpen" />
  </div>
</template>

<style scoped>
.app-shell {
  background-image: linear-gradient(180deg, #f8fafc 0%, #ffffff 100%);
  height: 100vh;
  max-height: 100vh;
  overflow: hidden;
}

main {
  min-height: 0;
  overflow: hidden;
  flex: 1;
}

.projects-panel {
  flex-shrink: 0;
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.content-panel {
  background-color: #ffffff;
  min-height: 0;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
</style>
