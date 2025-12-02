<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useStore } from 'vuex';
import AppHeader from './components/AppHeader.vue';
import CalendarHeader from './components/calendar/CalendarHeader.vue';
import UsersTree from './components/workloads/UsersTree.vue';
import WorkloadTimeline from './components/workloads/WorkloadTimeline.vue';
import SettingsModal from './components/settings/SettingsModal.vue';
import { getProjectColor } from './utils/colors';

const store = useStore();

// Древовидная структура фильтров для TreeSelect
const FILTER_TREE = [
  {
    key: 'role',
    label: 'По роли',
    children: [
      { key: 'role-modeller', label: 'Modeller', value: 'modeller', type: 'role' },
      { key: 'role-freelancer', label: 'Freelancer', value: 'freelancer', type: 'role' },
      { key: 'role-artist', label: 'Artist', value: 'artist', type: 'role' },
      { key: 'role-art_director', label: 'Art Director', value: 'art_director', type: 'role' },
      { key: 'role-project_manager', label: 'Project Manager', value: 'project_manager', type: 'role' },
    ]
  },
  {
    key: 'projects',
    label: 'По проектам',
    children: [
      { key: 'project-active', label: 'Активные', value: 'active', type: 'project' },
      { key: 'project-inactive', label: 'Неактивные', value: 'inactive', type: 'project' },
    ]
  }
];

const threeMonthsData = computed(() => store.getters['calendar/threeMonthsData']);
const allDates = computed(() => store.getters['calendar/allDates']);
const blockWidth = computed(() => store.getters['calendar/blockWidth']);

const workloads = computed(() => store.getters['workloads/users']);
const isLoading = computed(() => store.getters['workloads/isLoading']);
const error = computed(() => store.getters['workloads/error']);

const expandedUsers = ref(new Set());
const expandedProjects = ref(new Set());
const searchValue = ref('');
const isSettingsVisible = ref(false);

// Фильтры (древовидный множественный выбор)
const selectedFilters = ref(null);

// Проверка активности фильтров
const hasActiveFilters = computed(() => {
  return selectedFilters.value && Object.keys(selectedFilters.value).length > 0;
});

// Количество активных фильтров
const activeFiltersCount = computed(() => {
  if (!selectedFilters.value) return 0;
  let count = 0;
  Object.keys(selectedFilters.value).forEach(key => {
    if (selectedFilters.value[key].checked) {
      count++;
    }
  });
  return count;
});

const treeScrollRef = ref(null);
const timelineComponentRef = ref(null);
const calendarHeaderRef = ref(null);
let scrollSyncAttached = false;

const getTimelineScrollElement = () => timelineComponentRef.value?.getScrollElement?.() ?? null;

const syncVerticalScroll = (event) => {
  const treeEl = treeScrollRef.value;
  const timelineEl = getTimelineScrollElement();
  if (!treeEl || !timelineEl) return;

  if (event.target === treeEl) {
    timelineEl.scrollTop = treeEl.scrollTop;
  } else if (event.target === timelineEl) {
    treeEl.scrollTop = timelineEl.scrollTop;
  }
};

const detachScrollSync = () => {
  const treeEl = treeScrollRef.value;
  const timelineEl = getTimelineScrollElement();
  if (treeEl) {
    treeEl.removeEventListener('scroll', syncVerticalScroll);
  }
  if (timelineEl) {
    timelineEl.removeEventListener('scroll', syncVerticalScroll);
  }
  scrollSyncAttached = false;
};

const attachScrollSync = () => {
  if (scrollSyncAttached) {
    detachScrollSync();
  }
  const treeEl = treeScrollRef.value;
  const timelineEl = getTimelineScrollElement();
  if (!treeEl || !timelineEl) return;
  treeEl.addEventListener('scroll', syncVerticalScroll, { passive: true });
  timelineEl.addEventListener('scroll', syncVerticalScroll, { passive: true });
  scrollSyncAttached = true;
};

const ensureScrollSync = () => {
  nextTick(() => {
    attachScrollSync();
  });
};

const buildRows = (users = []) => {
  const rows = [];
  users.forEach((user) => {
    const totalTasks = user.projects?.reduce((acc, project) => acc + (project.tasks?.length ?? 0), 0) ?? 0;
    const projectNames = user.projects?.map((project) => project.projectName).filter(Boolean) ?? [];
    const projectPreview = projectNames.slice(0, 3).join(', ');
    const summarySegments = (user.summarySegments ?? []).map((segment) => {
      const project = user.projects?.find(p => p.projectId === segment.projectId);
      return {
        ...segment,
        color: getProjectColor(segment.projectId),
        projectName: project?.projectName ?? segment.projectName ?? 'Проект',
      };
    });

    rows.push({
      id: `user-${user.userId}`,
      type: 'user',
      level: 0,
      label: user.userName,
      canExpand: (user.projects?.length ?? 0) > 0,
      segments: summarySegments,
      meta: {
        userId: user.userId,
        role: user.userRole,
        projectsCount: user.projects?.length ?? 0,
        tasksCount: totalTasks,
        projectPreview,
        projectNames,
      },
    });

    if (!expandedUsers.value.has(user.userId)) {
      return;
    }

    user.projects?.forEach((project) => {
      const projectKey = `${user.userId}:${project.projectId}`;
      const projectSegments = (project.segments ?? []).map((segment) => ({
        ...segment,
        color: getProjectColor(project.projectId),
      }));

      rows.push({
        id: `project-${projectKey}`,
        type: 'project',
        level: 1,
        label: project.projectName,
        canExpand: (project.tasks?.length ?? 0) > 0,
        segments: projectSegments,
        meta: {
          userId: user.userId,
          projectId: project.projectId,
          projectKey,
          clientName: project.clientName,
          tasksCount: project.tasks?.length ?? 0,
        },
      });

      if (!expandedProjects.value.has(projectKey)) {
        return;
      }

      project.tasks?.forEach((task) => {
        rows.push({
          id: `task-${projectKey}-${task.taskId}`,
          type: 'task',
          level: 2,
          label: task.imageName ?? `Task #${task.taskId}`,
          canExpand: false,
          segments: [
            {
              projectName: task.projectName,
              startDate: task.startDate,
              endDate: task.endDate,
              color: task.status?.color ?? '#2563eb',
              textColor: task.status?.textColor ?? '#ffffff',
              label: task.status?.name,
              tooltip: `${task.status?.name ?? 'Задача'}: ${task.startDate} — ${task.endDate}`,
            },
          ],
          meta: {
            taskId: task.taskId,
            taskLabel: task.imageName ?? `Task #${task.taskId}`,
            statusName: task.status?.name,
            batchName: task.batchName,
            imageName: task.imageName,
            completed: task.completed,
          },
        });
      });
    });
  });
  return rows;
};

// Фильтрованные workloads
const filteredWorkloads = computed(() => {
  let result = workloads.value ?? [];
  
  if (!selectedFilters.value || Object.keys(selectedFilters.value).length === 0) {
    return result;
  }

  // Извлекаем выбранные фильтры из дерева
  const selectedRoles = [];
  const selectedProjectTypes = [];
  
  Object.keys(selectedFilters.value).forEach(key => {
    if (selectedFilters.value[key].checked) {
      const node = findNodeByKey(FILTER_TREE, key);
      if (node && node.type === 'role') {
        selectedRoles.push(node.value);
      } else if (node && node.type === 'project') {
        selectedProjectTypes.push(node.value);
      }
    }
  });
  
  // Фильтр по ролям
  if (selectedRoles.length > 0) {
    result = result.filter(user => selectedRoles.includes(user.userRole));
  }
  
  // Фильтр по типам проектов
  if (selectedProjectTypes.length > 0) {
    result = result.filter(user => {
      if (!user.projects || user.projects.length === 0) {
        return false;
      }
      
      const hasActive = selectedProjectTypes.includes('active');
      const hasInactive = selectedProjectTypes.includes('inactive');
      
      if (hasActive && hasInactive) {
        return true;
      } else if (hasActive) {
        return user.projects.some(project => project.projectIsActive);
      } else if (hasInactive) {
        return user.projects.some(project => !project.projectIsActive);
      }
      
      return true;
    });
  }
  
  return result;
});

// Вспомогательная функция для поиска узла в дереве
const findNodeByKey = (nodes, key) => {
  for (const node of nodes) {
    if (node.key === key) {
      return node;
    }
    if (node.children) {
      const found = findNodeByKey(node.children, key);
      if (found) return found;
    }
  }
  return null;
};

const rows = computed(() => buildRows(filteredWorkloads.value ?? []));

const fetchWorkloads = async () => {
  const months = threeMonthsData.value;
  if (!months || !months.length) {
    return;
  }
  const from = months[0]?.dates?.[0];
  const lastMonth = months[months.length - 1];
  const to = lastMonth?.dates?.[lastMonth.dates.length - 1];
  if (!from || !to) {
    return;
  }
  try {
    const params = { from, to };
    if (searchValue.value?.trim()) {
      params.search = searchValue.value.trim();
    }
    await store.dispatch('workloads/fetchWorkloads', params);
  } catch (err) {
    console.error(err);
  }
};

watch(
  threeMonthsData,
  () => {
    fetchWorkloads();
  },
  { immediate: true, deep: true },
);

watch(
  workloads,
  (next) => {
    const validUserIds = new Set(next?.map((user) => user.userId) ?? []);
    if (expandedUsers.value.size) {
      expandedUsers.value = new Set(
        Array.from(expandedUsers.value).filter((id) => validUserIds.has(id)),
      );
    }

    const validProjectKeys = new Set();
    next?.forEach((user) => {
      user.projects?.forEach((project) => {
        validProjectKeys.add(`${user.userId}:${project.projectId}`);
      });
    });
    if (expandedProjects.value.size) {
      expandedProjects.value = new Set(
        Array.from(expandedProjects.value).filter((key) => validProjectKeys.has(key)),
      );
    }

    ensureScrollSync();
  },
  { deep: true },
);

const handleToggleUser = (userId) => {
  if (!userId) return;
  const next = new Set(expandedUsers.value);
  if (next.has(userId)) {
    next.delete(userId);
  } else {
    next.add(userId);
  }
  expandedUsers.value = next;
  ensureScrollSync();
};

const handleToggleProject = (projectKey) => {
  if (!projectKey) return;
  const next = new Set(expandedProjects.value);
  if (next.has(projectKey)) {
    next.delete(projectKey);
  } else {
    next.add(projectKey);
  }
  expandedProjects.value = next;
  ensureScrollSync();
};

const handleRefresh = () => {
  fetchWorkloads();
};

let searchDebounceTimeout = null;

const handleSearchInput = () => {
  if (searchDebounceTimeout) {
    clearTimeout(searchDebounceTimeout);
  }
  searchDebounceTimeout = setTimeout(() => {
    fetchWorkloads();
  }, 300);
};

const handleOpenSettings = () => {
  isSettingsVisible.value = true;
};

const handleScrollToToday = () => {
  if (calendarHeaderRef.value) {
    calendarHeaderRef.value.scrollToToday();
  }
};

const clearFilters = () => {
  selectedFilters.value = null;
};

onMounted(() => {
  ensureScrollSync();
});

onUnmounted(() => {
  detachScrollSync();
  if (searchDebounceTimeout) {
    clearTimeout(searchDebounceTimeout);
  }
});
</script>

<template>
  <div class="app-shell">
    <AppHeader @open-settings="handleOpenSettings" @scroll-to-today="handleScrollToToday" />
    <main class="app-main">
      <div v-if="error" class="error-banner">
        <span>Не удалось загрузить данные: {{ error.message ?? error }}</span>
      </div>
      <div class="board">
        <section class="list-panel">
          <div class="list-controls">
            <div class="control-section">
              <div class="control-input">
                <input
                  v-model="searchValue"
                  type="text"
                  placeholder="Найти сотрудника, проект или задачу..."
                  @input="handleSearchInput"
                />
              </div>
            </div>
            <div class="control-section">
              <div class="control-input control-filters">
                <TreeSelect
                  v-model="selectedFilters"
                  :options="FILTER_TREE"
                  selectionMode="checkbox"
                  :placeholder="hasActiveFilters ? `Фильтры (${activeFiltersCount})` : 'Выберите фильтры'"
                  class="filter-tree-select"
                  display="chip"
                />
                <button 
                  v-if="hasActiveFilters" 
                  class="clear-filters-icon"
                  type="button"
                  @click="clearFilters"
                  title="Очистить фильтры"
                >
                  <i class="pi pi-times-circle"></i>
                </button>
              </div>
            </div>
          </div>
          <div ref="treeScrollRef" class="list-scroll">
            <UsersTree
              :rows="rows"
              :expanded-users="expandedUsers"
              :expanded-projects="expandedProjects"
              @toggle-user="handleToggleUser"
              @toggle-project="handleToggleProject"
            />
          </div>
        </section>
        <section class="timeline-panel">
          <CalendarHeader ref="calendarHeaderRef" />
          <WorkloadTimeline
            ref="timelineComponentRef"
            :rows="rows"
            :dates="allDates"
            :block-width="blockWidth"
            :loading="isLoading"
          />
        </section>
      </div>
    </main>
    <SettingsModal v-model:visible="isSettingsVisible" />
    <Toast />
    <ConfirmDialog />
  </div>
</template>

<style>
html, body {
  margin: 0;
  padding: 0;
  height: 100%;
  overflow: hidden;
}

#app {
  height: 100%;
  overflow: hidden;
}
</style>

<style scoped>
.app-shell {
  height: 100vh;
  max-height: 100vh;
  background: #f8fafc;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.app-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 0;
  gap: 0;
  min-height: 0;
  overflow: hidden;
}

.page-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.5rem;
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
  flex-shrink: 0;
}

.toolbar-text h1 {
  margin: 0;
  font-size: 1.35rem;
  color: #0f172a;
}

.toolbar-text p {
  margin: 0.25rem 0 0;
  color: #475569;
  font-size: 0.95rem;
}

.refresh-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  border: none;
  background: #0ea5e9;
  color: #fff;
  font-weight: 600;
  border-radius: 999px;
  padding: 0.6rem 1.4rem;
  cursor: pointer;
  transition: background 0.2s ease, transform 0.15s ease;
}

.refresh-btn:hover {
  background: #38bdf8;
}

.refresh-btn:active {
  transform: scale(0.98);
}

.error-banner {
  background: #fee2e2;
  color: #b91c1c;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  font-weight: 600;
}

.board {
  display: flex;
  flex: 1;
  min-height: 0;
  background: #ffffff;
  overflow: hidden;
  border-top: 1px solid #e2e8f0;
  border-bottom: 1px solid #e2e8f0;
  --row-height: 80px;
  --header-height: clamp(120px, 14vh, 160px);
  --control-section-height: calc(var(--header-height) / 2);
}

.list-panel {
  width: clamp(400px, 30vw, 600px);
  border-right: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
  min-height: 0;
  max-height: 100%;
  overflow: hidden;
}

.list-controls {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.control-section {
  padding: 0.5rem 1rem;
  border-bottom: 1px solid #e2e8f0;
  background: #f8fafc;
  height: var(--control-section-height);
  min-height: var(--control-section-height);
  max-height: var(--control-section-height);
  display: flex;
  flex-direction: column;
  justify-content: center;
  flex-shrink: 0;
  box-sizing: border-box;
}

.control-input {
  position: relative;
  display: flex;
  align-items: center;
  overflow: hidden;
}

.control-input input {
  width: 100%;
  border: 1px solid #e2e8f0;
  border-radius: 999px;
  padding: 0.5rem 0.9rem;
  background: #ffffff;
  font-size: clamp(0.8rem, 1vw, 0.9rem);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.control-input input:disabled {
  background: #e2e8f0;
  color: #94a3b8;
  cursor: not-allowed;
}

.control-input input:focus {
  outline: none;
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.15);
}

.list-scroll {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  min-height: 0;
}

.timeline-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  min-height: 0;
  max-height: 100%;
  overflow: hidden;
}

.timeline-panel :deep(.calendar-container) {
  border-bottom: 1px solid #e2e8f0;
  box-shadow: none;
}

.control-filters {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0;
  border: none;
  flex: 1;
  overflow: hidden;
  max-width: 100%;
}

.filter-tree-select {
  flex: 1;
  min-width: 0;
  max-width: 100%;
  overflow: hidden;
}

.filter-tree-select :deep(.p-treeselect) {
  font-size: clamp(0.8rem, 1vw, 0.9rem);
  border: none;
  background: transparent;
  box-shadow: none;
  padding: 0;
  width: 100%;
  max-width: 100%;
  overflow: hidden;
}

.filter-tree-select :deep(.p-treeselect:hover) {
  border: none;
}

.filter-tree-select :deep(.p-treeselect:focus) {
  border: none;
  box-shadow: none;
}

.filter-tree-select :deep(.p-treeselect-label) {
  padding: 0.25rem 0.4rem;
  color: #475569;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 100%;
}

.filter-tree-select :deep(.p-treeselect-trigger) {
  color: #0ea5e9;
}

.filter-tree-select :deep(.p-chip) {
  background: #0ea5e9;
  color: #ffffff;
  border-radius: 4px;
  font-weight: 600;
  font-size: clamp(0.7rem, 0.9vw, 0.8rem);
  padding: 0.2rem 0.5rem;
  max-width: min(120px, 20%);
  overflow: hidden;
}

.filter-tree-select :deep(.p-chip .p-chip-remove-icon) {
  color: rgba(255, 255, 255, 0.9);
  font-size: 0.65rem;
}

.filter-tree-select :deep(.p-chip .p-chip-text) {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.clear-filters-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border: none;
  background: transparent;
  color: #94a3b8;
  cursor: pointer;
  border-radius: 50%;
  transition: all 0.2s ease;
  flex-shrink: 0;
}

.clear-filters-icon:hover {
  background: #fee2e2;
  color: #ef4444;
}

.clear-filters-icon i {
  font-size: 0.95rem;
}

.filter-tree-select :deep(.p-treeselect-label.p-placeholder) {
  color: #94a3b8;
  font-weight: 400;
}

.filter-tree-select :deep(.p-tree-container) {
  max-height: 300px;
}

.filter-tree-select :deep(.p-tree .p-tree-toggler) {
  color: #0ea5e9;
}

.filter-tree-select :deep(.p-tree .p-treenode-label) {
  font-size: 0.9rem;
  color: #475569;
}

.filter-tree-select :deep(.p-tree .p-checkbox .p-checkbox-box) {
  border-color: #cbd5e1;
}

.filter-tree-select :deep(.p-tree .p-checkbox .p-checkbox-box.p-highlight) {
  background: #0ea5e9;
  border-color: #0ea5e9;
}
</style>
