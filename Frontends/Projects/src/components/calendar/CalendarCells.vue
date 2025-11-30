<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick, watch, reactive } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';

const props = defineProps({
  projects: {
    type: Array,
    default: () => [],
  },
  expandedProjects: {
    type: Set,
    default: () => new Set(),
  },
  batchesByProject: {
    type: Map,
    default: () => new Map(),
  },
  expandedBatches: {
    type: Set,
    default: () => new Set(),
  },
  imagesByBatch: {
    type: Map,
    default: () => new Map(),
  },
  loadingBatches: {
    type: Set,
    default: () => new Set(),
  },
  loadingImages: {
    type: Set,
    default: () => new Set(),
  },
  isLoadingMore: {
    type: Boolean,
    default: false,
  },
  hasMore: {
    type: Boolean,
    default: true,
  },
});

const emit = defineEmits(['cell-click', 'cell-context-menu']);

const store = useStore();
const toast = useToast();
const cellsContainer = ref(null);
const cellsScrollWrapper = ref(null);
const contextMenuRef = ref(null);

const allDates = computed(() => store.getters['calendar/allDates']);
const blockWidth = computed(() => store.getters['calendar/blockWidth']);
const isWeekendFn = computed(() => store.getters['calendar/isWeekend']);

const tasksCollections = computed(() => store.state.tasks.collections);
const selectedStatusId = computed(() => store.state.statuses.selectedStatusId);
const selectedStatus = computed(() => {
  if (!selectedStatusId.value) return null;
  return store.state.statuses.items.find((status) => status.id === selectedStatusId.value) ?? null;
});

const weekendStatus = computed(() => {
  return (
    store.state.statuses.items.find(
      (status) => status.name?.trim().toLowerCase() === 'weekend',
    ) ?? null
  );
});

const dateIndexMap = computed(() => {
  const map = new Map();
  allDates.value.forEach((date, index) => map.set(date, index));
  return map;
});

const buildImageScopeKey = ({ projectId, batchId, imageId, id }) =>
  `${projectId}:${batchId}:${imageId ?? id}`;

const buildCellKey = ({ projectId, batchId, imageId, date }) =>
  `${projectId}:${batchId}:${imageId}:${date}`;

const virtualItems = computed(() => {
  const items = [];
  let imageRowIndex = 0;
  
  props.projects.forEach((project) => {
    items.push({
      type: 'project',
      data: project,
    });
    
    if (props.expandedProjects.has(project.id)) {
      const batches = props.batchesByProject.get(project.id) || [];
      
      if (props.loadingBatches.has(project.id)) {
        items.push({
          type: 'loading-batches',
          data: { projectId: project.id },
        });
      } else if (batches.length === 0) {
        items.push({
          type: 'empty-batches',
          data: { projectId: project.id },
        });
      } else {
        batches.forEach((batch) => {
          const batchKey = `${project.id}-${batch.id}`;
          
          items.push({
            type: 'batch',
            data: { ...batch, projectId: project.id },
          });
          
          if (props.expandedBatches.has(batchKey)) {
            const images = props.imagesByBatch.get(batchKey) || [];
            
            if (props.loadingImages.has(batchKey)) {
              items.push({
                type: 'loading-images',
                data: { projectId: project.id, batchId: batch.id },
              });
            } else if (images.length === 0) {
              items.push({
                type: 'empty-images',
                data: { projectId: project.id, batchId: batch.id },
              });
            } else {
              images.forEach((image) => {
                items.push({
                  type: 'image',
                  rowIndex: imageRowIndex,
                  data: { 
                    ...image, 
                    projectId: project.id, 
                    projectName: project.name,
                    batchId: batch.id,
                    batchName: batch.name,
                  },
                });
                imageRowIndex += 1;
              });
            }
          }
        });
      }
    }
  });
  
  return items;
});

const imageRows = computed(() =>
  virtualItems.value
    .filter((item) => item.type === 'image')
    .map((item) => ({ rowIndex: item.rowIndex, data: item.data })),
);

const imageRowsByIndex = computed(() => {
  const rows = [];
  imageRows.value.forEach((row) => {
    rows[row.rowIndex] = row.data;
  });
  return rows;
});

const imagesById = computed(() => {
  const map = new Map();
  imageRows.value.forEach(({ data }) => {
    map.set(data.id, data);
  });
  return map;
});

const fetchedImageKeys = new Set();

const ensureTasksForRow = async (row) => {
  const scope = {
    projectId: row.data.projectId,
    batchId: row.data.batchId,
    imageId: row.data.id,
  };
  const key = buildImageScopeKey(scope);
  if (fetchedImageKeys.has(key)) return;
  fetchedImageKeys.add(key);
  try {
    await store.dispatch('tasks/fetchForImage', scope);
  } catch (error) {
    fetchedImageKeys.delete(key);
    toast.add({
      severity: 'error',
      summary: 'Не удалось загрузить задачи',
      detail: error.message ?? 'Попробуйте обновить страницу.',
      life: 4000,
    });
  }
};

watch(
  imageRows,
  (rows) => {
    rows.forEach((row) => {
      void ensureTasksForRow(row);
    });
  },
  { immediate: true, deep: true },
);

const selectedCells = ref(new Map());
const selectionAnchor = ref(null);
const dragStartCell = ref(null);
const isDraggingSelection = ref(false);
const isPerformingAction = ref(false);

const selectedCellsCount = computed(() => selectedCells.value.size);
const selectedCellsList = computed(() => Array.from(selectedCells.value.values()));
const selectionTasks = computed(() => {
  const map = new Map();
  selectedCellsList.value.forEach((cell) => {
    const task = getTaskForCell(cell);
    if (task) {
      map.set(task.id, task);
    }
  });
  return Array.from(map.values());
});
const selectedImageIds = computed(() => {
  const ids = new Set();
  selectedCellsList.value.forEach((cell) => {
    if (cell?.imageId != null) {
      ids.add(cell.imageId);
    }
  });
  return Array.from(ids);
});
const selectedImages = computed(() => {
  const result = [];
  selectedImageIds.value.forEach((imageId) => {
    const image = imagesById.value?.get(imageId);
    if (image) {
      result.push(image);
    }
  });
  return result;
});
const selectionTaskCount = computed(() => selectionTasks.value.length);
const selectionSummary = computed(() => {
  if (!selectedCellsCount.value) return 'Нет выделения';
  return `${selectedCellsCount.value} ячеек`;
});
const selectionHasTasks = computed(() => selectionTaskCount.value > 0);
const selectionAllCompleted = computed(
  () =>
    selectionHasTasks.value &&
    selectionTasks.value.every((task) => task.completed),
);
const toggleCompletionLabel = computed(() =>
  selectionAllCompleted.value ? 'Открыть задачу' : 'Complete the task',
);
const canFillSelection = computed(
  () => !!selectedStatusId.value && selectedCellsCount.value > 0,
);
const canClearSelection = computed(() => selectionHasTasks.value);

const selectionImageUserOptions = computed(() => {
  const map = new Map();
  selectedImages.value.forEach((image) => {
    (image?.users ?? []).forEach((user) => {
      const option = createUserOption(user);
      if (option && !map.has(option.value)) {
        map.set(option.value, option);
      }
    });
  });
  return Array.from(map.values());
});

const selectionTaskUserOptions = computed(() => {
  const map = new Map();
  selectionTasks.value.forEach((task) => {
    (task?.users ?? []).forEach((user) => {
      const option = createUserOption(user);
      if (option && !map.has(option.value)) {
        map.set(option.value, option);
      }
    });
  });
  return Array.from(map.values());
});

const availableUserOptions = computed(() => {
  const map = new Map();
  selectionImageUserOptions.value.forEach((option) => {
    map.set(option.value, option);
  });
  selectionTaskUserOptions.value.forEach((option) => {
    if (!map.has(option.value)) {
      map.set(option.value, option);
    }
  });
  return Array.from(map.values()).sort((a, b) => a.label.localeCompare(b.label, 'ru'));
});

const userOptionsMap = computed(() => {
  const map = new Map();
  availableUserOptions.value.forEach((option) => {
    map.set(option.value, option);
  });
  return map;
});

const existingTaskUserIds = computed(() => {
  const ids = new Set();
  selectionTasks.value.forEach((task) => {
    (task?.users ?? []).forEach((user) => {
      const id = user?.userId ?? user?.id;
      if (id) {
        ids.add(id);
      }
    });
  });
  return Array.from(ids);
});

const canAttachUsers = computed(() => selectionHasTasks.value);

const setSelectionFromCells = (cells) => {
  const entries = [];
  const seen = new Set();
  cells.forEach((cell) => {
    const key = buildCellKey(cell);
    if (!seen.has(key)) {
      seen.add(key);
      entries.push([key, cell]);
    }
  });
  selectedCells.value = new Map(entries);
};

const addCellsToSelection = (cells) => {
  const next = new Map(selectedCells.value);
  cells.forEach((cell) => {
    next.set(buildCellKey(cell), cell);
  });
  selectedCells.value = next;
};

const toggleCellSelection = (cell) => {
  const key = buildCellKey(cell);
  const next = new Map(selectedCells.value);
  if (next.has(key)) {
    next.delete(key);
  } else {
    next.set(key, cell);
  }
  selectedCells.value = next;
};

const clearSelection = () => {
  selectedCells.value = new Map();
  selectionAnchor.value = null;
};

const buildCellInfo = (imageData, date, rowIndex) => ({
  projectId: imageData.projectId,
  batchId: imageData.batchId,
  imageId: imageData.id,
  projectName: imageData.projectName,
  batchName: imageData.batchName,
  imageName: imageData.name,
  date,
  rowIndex,
  colIndex: dateIndexMap.value.get(date) ?? -1,
});

const buildCellsRectangle = (start, end) => {
  if (!start || !end) return [];
  const minRow = Math.min(start.rowIndex, end.rowIndex);
  const maxRow = Math.max(start.rowIndex, end.rowIndex);
  const minCol = Math.min(start.colIndex, end.colIndex);
  const maxCol = Math.max(start.colIndex, end.colIndex);
  const cells = [];

  for (let row = minRow; row <= maxRow; row += 1) {
    const rowData = imageRowsByIndex.value[row];
    if (!rowData) continue;
    for (let col = minCol; col <= maxCol; col += 1) {
      const date = allDates.value[col];
      if (!date) continue;
      cells.push(buildCellInfo(rowData, date, row));
    }
  }

  return cells;
};

const getTaskForCell = (cell) => {
  const scopeKey = buildImageScopeKey(cell);
  const collection = tasksCollections.value[scopeKey];
  if (!collection) return null;
  const meta = collection.byDate?.[cell.date];
  if (!meta) return null;
  return collection.tasksById?.[meta.taskId] ?? null;
};

const getTaskForImageDate = (imageData, date) => {
  const key = buildImageScopeKey(imageData);
  const collection = tasksCollections.value[key];
  if (!collection) return null;
  const meta = collection.byDate?.[date];
  if (!meta) return null;
  return collection.tasksById?.[meta.taskId] ?? null;
};

const isWeekend = (date) => isWeekendFn.value(date);

const getSelectionKeyFromData = (imageData, date) =>
  buildCellKey({
    projectId: imageData.projectId,
    batchId: imageData.batchId,
    imageId: imageData.id,
    date,
  });

const resolveCellClasses = (imageData, date) => {
  const task = getTaskForImageDate(imageData, date);
  const isWeekendDay = isWeekend(date);
  
  return {
    weekend: isWeekendDay,
    selected: selectedCells.value.has(getSelectionKeyFromData(imageData, date)),
    'has-task': !!task && !isWeekendDay,
    completed: !!task?.completed && !isWeekendDay,
    'weekend-auto': weekendStatus.value && isWeekendDay,
  };
};

const resolveCellStyle = (imageData, date) => {
  const task = getTaskForImageDate(imageData, date);
  
  // Выходные всегда имеют приоритет
  if (weekendStatus.value && isWeekend(date)) {
    return {
      '--weekend-auto-color': weekendStatus.value.color,
      '--weekend-auto-text-color': weekendStatus.value.textColor,
    };
  }
  
  if (task) {
    return {
      '--cell-color': task.status?.color || '#cfe8fc',
      '--cell-text-color': task.status?.textColor || '#0f172a',
    };
  }
  
  return {};
};

const normalizeRoleKey = (role) => (role ?? '').toLowerCase();
const roleLabelsMap = {
  artist: 'Artist',
  modeller: 'Modeller',
  art_director: 'Art Director',
  project_manager: 'Project Manager',
  freelancer: 'Freelancer',
};

const getRoleLabel = (role) => roleLabelsMap[normalizeRoleKey(role)] ?? 'Участник';

const createUserOption = (user) => {
  const id = user?.userId ?? user?.id ?? user?.value;
  if (!id) return null;
  const rawLabel = user?.name ?? user?.label ?? user?.fullName ?? '';
  const label = rawLabel.trim() || `ID ${id}`;
  const role = normalizeRoleKey(user?.role ?? user?.Role ?? 'artist');
  return {
    label,
    value: id,
    role,
    roleLabel: getRoleLabel(role),
  };
};

const getTaskUsers = (imageData, date) => {
  const task = getTaskForImageDate(imageData, date);
  return task?.users ?? [];
};

const formatUsersTooltip = (users) => {
  if (!users?.length) return '';
  return users
    .map((user) => `${user.name ?? 'Без имени'} • ${getRoleLabel(user.role)}`)
    .join('\n');
};

const getCellTooltip = (imageData, date) => {
  const users = getTaskUsers(imageData, date);
  if (!users.length) return '';
  return formatUsersTooltip(users);
};

// Группировка смежных ячеек через выходные
// Находит все ячейки, связанные с данной ячейкой через задачу того же статуса
// ВАЖНО: 
// 1. Пользователь МОЖЕТ выделять и закрашивать выходные
// 2. Если выделил выходные - они закрашиваются (создается задача на выходной)
// 3. Ячейки через выходные объединяются в одну группу (одну задачу)
// 4. Если закрывать/аттачить юзеров к ячейке - действие применяется ко всей группе через выходные
const getConnectedCellsForTask = (cell) => {
  const result = [];
  const task = getTaskForCell(cell);
  if (!task) return [cell];
  
  const visited = new Set();
  const queue = [cell];
  
  while (queue.length > 0) {
    const current = queue.shift();
    const currentKey = buildCellKey(current);
    
    if (visited.has(currentKey)) continue;
    visited.add(currentKey);
    result.push(current);
    
    const currentDateIndex = dateIndexMap.value.get(current.date);
    if (currentDateIndex === undefined) continue;
    
    const imageData = imagesById.value.get(current.imageId);
    if (!imageData) continue;
    
    // Проверяем влево и вправо
    for (let offset = -1; offset <= 1; offset += 2) {
      let checkIndex = currentDateIndex + offset;
      let foundWeekend = false;
      
      // Ищем следующую рабочую ячейку, пропуская выходные
      while (checkIndex >= 0 && checkIndex < allDates.value.length) {
        const checkDate = allDates.value[checkIndex];
        
        // Если это выходной - продолжаем поиск
        if (isWeekend(checkDate)) {
          foundWeekend = true;
          checkIndex += offset;
          continue;
        }
        
        // Нашли рабочий день - проверяем задачу
        const checkTask = getTaskForImageDate(imageData, checkDate);
        
        if (checkTask && checkTask.id === task.id) {
          // Та же задача - добавляем в очередь
          const cellInfo = buildCellInfo(imageData, checkDate, current.rowIndex);
          const cellKey = buildCellKey(cellInfo);
          if (!visited.has(cellKey)) {
            queue.push(cellInfo);
          }
        }
        // В любом случае прерываем - нашли рабочий день
        break;
      }
    }
  }
  
  return result;
};

// Расширяет выделение на все связанные ячейки в группе
const expandSelectionToGroups = (cells) => {
  const expanded = new Map();
  const cellsList = Array.isArray(cells) ? cells : Array.from(cells);
  
  cellsList.forEach(cell => {
    const connected = getConnectedCellsForTask(cell);
    connected.forEach(c => {
      expanded.set(buildCellKey(c), c);
    });
  });
  
  return Array.from(expanded.values());
};

const handleGlobalPointerUp = () => {
  isDraggingSelection.value = false;
  dragStartCell.value = null;
  window.removeEventListener('pointerup', handleGlobalPointerUp);
};

const handleCellPointerDown = (event, item, date) => {
  if (event.button !== 0) return;
  const cellInfo = buildCellInfo(item.data, date, item.rowIndex);
  const withCtrl = event.ctrlKey || event.metaKey;

  if (event.shiftKey && selectionAnchor.value) {
    const rangeCells = buildCellsRectangle(selectionAnchor.value, cellInfo);
    if (withCtrl) {
      addCellsToSelection(rangeCells);
    } else {
      setSelectionFromCells(rangeCells);
    }
  } else if (withCtrl) {
    toggleCellSelection(cellInfo);
  } else {
    setSelectionFromCells([cellInfo]);
  }

  selectionAnchor.value = cellInfo;
  dragStartCell.value = cellInfo;
  isDraggingSelection.value = true;
  window.addEventListener('pointerup', handleGlobalPointerUp);
};

const handleCellPointerEnter = (event, item, date) => {
  if (!isDraggingSelection.value || !dragStartCell.value) return;
  if (event.buttons === 0) {
    handleGlobalPointerUp();
    return;
  }
  const cellInfo = buildCellInfo(item.data, date, item.rowIndex);
  const rangeCells = buildCellsRectangle(dragStartCell.value, cellInfo);
  setSelectionFromCells(rangeCells);
};

const handleCellClick = (event, imageData, date) => {
  emit('cell-click', {
    event,
    projectId: imageData.projectId,
    batchId: imageData.batchId,
    imageId: imageData.id,
    date,
  });
};

const handleCellContextMenu = (event, item, date) => {
  event.preventDefault();
  const cellInfo = buildCellInfo(item.data, date, item.rowIndex);
  if (!selectedCells.value.has(buildCellKey(cellInfo))) {
    setSelectionFromCells([cellInfo]);
    selectionAnchor.value = cellInfo;
  }
  emit('cell-context-menu', {
    event,
    projectId: item.data.projectId,
    batchId: item.data.batchId,
    imageId: item.data.id,
    date,
  });
  openContextMenu(event);
};

const contextMenuState = reactive({
  visible: false,
  x: 0,
  y: 0,
});

const openContextMenu = (event) => {
  contextMenuState.visible = true;
  contextMenuState.x = event.clientX;
  contextMenuState.y = event.clientY;
  nextTick(() => {
    const menu = contextMenuRef.value;
    if (!menu) return;
    const rect = menu.getBoundingClientRect();
    if (rect.right > window.innerWidth) {
      contextMenuState.x = Math.max(8, window.innerWidth - rect.width - 8);
    }
    if (rect.bottom > window.innerHeight) {
      contextMenuState.y = Math.max(8, window.innerHeight - rect.height - 8);
    }
  });
};

const closeContextMenu = () => {
  contextMenuState.visible = false;
};

watch(selectedCellsCount, (count) => {
  if (count === 0) {
    closeContextMenu();
  }
});

const handleDocumentClick = (event) => {
  if (!contextMenuState.visible) return;
  if (contextMenuRef.value && !contextMenuRef.value.contains(event.target)) {
    closeContextMenu();
  }
};

const handleKeyDown = (event) => {
  if (event.key === 'Escape') {
    closeContextMenu();
    clearSelection();
  }
};

const performSafely = async (callback) => {
  if (isPerformingAction.value) return;
  isPerformingAction.value = true;
  try {
    await callback();
  } finally {
    isPerformingAction.value = false;
  }
};

const handleFillSelection = async () => {
  if (!selectedStatusId.value) {
    toast.add({
      severity: 'warn',
      summary: 'Выберите статус',
      detail: 'Чтобы закрасить ячейки, сначала выберите статус вверху.',
      life: 4000,
    });
    return;
  }
  if (!selectedCellsList.value.length) return;
  await performSafely(async () => {
    try {
      // Группируем ячейки по imageId для обработки
      const cellsByImage = new Map();
      selectedCellsList.value.forEach(cell => {
        const key = `${cell.projectId}:${cell.batchId}:${cell.imageId}`;
        if (!cellsByImage.has(key)) {
          cellsByImage.set(key, []);
        }
        cellsByImage.get(key).push(cell);
      });
      
      const result = await store.dispatch('tasks/applyStatusToCells', {
        cells: selectedCellsList.value,
        statusId: selectedStatusId.value,
        groupByWeekends: true, // Флаг для группировки через выходные
      });
      toast.add({
        severity: 'success',
        summary: 'Статусы обновлены',
        detail: `Создано ${result.created}, обновлено ${result.updated}.`,
        life: 3500,
      });
      closeContextMenu();
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Не удалось закрасить ячейки',
        detail: error.message ?? 'Попробуйте ещё раз.',
        life: 4500,
      });
    }
  });
};

const handleClearSelection = async () => {
  if (!selectionHasTasks.value) {
    toast.add({
      severity: 'info',
      summary: 'Нечего очищать',
      detail: 'В выбранных ячейках нет задач.',
      life: 3500,
    });
    return;
  }
  await performSafely(async () => {
    try {
      // Расширяем выделение на все связанные ячейки в группах
      const expandedCells = expandSelectionToGroups(selectedCellsList.value);
      
      const result = await store.dispatch('tasks/clearCells', {
        cells: expandedCells.length > 0 ? expandedCells : selectedCellsList.value,
        expandGroups: true,
      });
      toast.add({
        severity: 'success',
        summary: 'Удаление выполнено',
        detail: result.removed
          ? `Удалено ${result.removed} задач.`
          : 'Не удалось найти задачи для удаления.',
        life: 3500,
      });
      closeContextMenu();
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Не удалось очистить ячейки',
        detail: error.message ?? 'Попробуйте ещё раз.',
        life: 4500,
      });
    }
  });
};

const handleToggleCompletion = async () => {
  if (!selectionHasTasks.value) {
    toast.add({
      severity: 'info',
      summary: 'Нет задач для изменения статуса',
      detail: 'Сначала выделите закрашенные ячейки.',
      life: 3500,
    });
    return;
  }
  const targetCompleted = !selectionAllCompleted.value;
  await performSafely(async () => {
    try {
      // Расширяем выделение на все связанные ячейки в группах
      const expandedCells = expandSelectionToGroups(selectedCellsList.value);
      
      const result = await store.dispatch('tasks/completeCells', {
        cells: expandedCells.length > 0 ? expandedCells : selectedCellsList.value,
        completed: targetCompleted,
        expandGroups: true,
      });
      toast.add({
        severity: 'success',
        summary: targetCompleted ? 'Задачи завершены' : 'Задачи открыты',
        detail: result.updated
          ? `Обновлено ${result.updated} задач.`
          : 'Не удалось обновить задачи.',
        life: 3500,
      });
      closeContextMenu();
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: targetCompleted
          ? 'Не удалось завершить задачи'
          : 'Не удалось открыть задачи',
        detail: error.message ?? 'Попробуйте ещё раз.',
        life: 4500,
      });
    }
  });
};

const attachUsersDialog = reactive({
  visible: false,
  selectedUserIds: [],
  roleFilter: 'all',
  isSaving: false,
});

watch(
  () => attachUsersDialog.visible,
  (visible) => {
    if (visible) {
      attachUsersDialog.roleFilter = 'all';
      attachUsersDialog.selectedUserIds = [...existingTaskUserIds.value];
    } else {
      attachUsersDialog.selectedUserIds = [];
      attachUsersDialog.roleFilter = 'all';
      attachUsersDialog.isSaving = false;
    }
  },
);

const roleFilterOptions = [
  { label: 'Все роли', value: 'all' },
  { label: 'Artists', value: 'artist' },
  { label: 'Modellers', value: 'modeller' },
  { label: 'Art Directors', value: 'art_director' },
  { label: 'Project Managers', value: 'project_manager' },
  { label: 'Freelancers', value: 'freelancer' },
];

const filteredUserOptions = computed(() => {
  if (attachUsersDialog.roleFilter === 'all') {
    return availableUserOptions.value;
  }
  return availableUserOptions.value.filter(
    (option) => option.role === attachUsersDialog.roleFilter,
  );
});

const openAttachUsersDialog = () => {
  if (!selectionHasTasks.value) {
    toast.add({
      severity: 'info',
      summary: 'Нет задач для привязки участников',
      detail: 'Сначала закрасьте ячейки.',
      life: 3500,
    });
    return;
  }
  attachUsersDialog.visible = true;
};

const closeAttachUsersDialog = () => {
  attachUsersDialog.visible = false;
};

const confirmAttachUsers = async () => {
  const hadSelection = attachUsersDialog.selectedUserIds.length > 0;
  const normalizedUsers = attachUsersDialog.selectedUserIds
    .map((userId) => {
      const option = userOptionsMap.value.get(userId);
      if (option) {
        return {
          userId: option.value,
          role: option.role ?? 'artist',
        };
      }
      return {
        userId,
        role: 'artist',
      };
    })
    .filter(Boolean);

  if (hadSelection && !normalizedUsers.length) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось подготовить пользователей',
      detail: 'Попробуйте выбрать участников снова.',
      life: 4000,
    });
    return;
  }
  attachUsersDialog.isSaving = true;
  try {
    // Расширяем выделение на все связанные ячейки в группах
    const expandedCells = expandSelectionToGroups(selectedCellsList.value);
    
    const result = await store.dispatch('tasks/attachUsersToCells', {
      cells: expandedCells.length > 0 ? expandedCells : selectedCellsList.value,
      users: normalizedUsers,
      expandGroups: true,
    });
    toast.add({
      severity: 'success',
      summary: 'Пользователи назначены',
      detail: result.updated
        ? `Обновлено ${result.updated} задач.`
        : 'Не удалось обновить задачи.',
      life: 3500,
    });
    attachUsersDialog.visible = false;
    closeContextMenu();
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось назначить пользователей',
      detail: error.message ?? 'Попробуйте ещё раз.',
      life: 4500,
    });
  } finally {
    attachUsersDialog.isSaving = false;
  }
};

// Синхронизация скролла
const syncScrollFromCalendarHeader = () => {
  const calendarHeader = document.getElementById('calendar-dates-scroll');
  if (calendarHeader && cellsContainer.value) {
    cellsContainer.value.scrollLeft = calendarHeader.scrollLeft;
  }
};

const handleScroll = (event) => {
  const calendarHeader = document.getElementById('calendar-dates-scroll');
  if (calendarHeader) {
    calendarHeader.scrollLeft = event.target.scrollLeft;
  }
};

onMounted(() => {
  if (cellsContainer.value) {
    cellsContainer.value.addEventListener('scroll', handleScroll);
  }
  
  const calendarHeader = document.getElementById('calendar-dates-scroll');
  if (calendarHeader) {
    calendarHeader.addEventListener('scroll', syncScrollFromCalendarHeader);
  }
  
  window.addEventListener('click', handleDocumentClick);
  window.addEventListener('keydown', handleKeyDown);

  // Синхронизируем скролл сразу и после рендеринга
  nextTick(() => {
    syncScrollFromCalendarHeader();
    // Дополнительная синхронизация с задержкой для плавной прокрутки
    setTimeout(() => {
      syncScrollFromCalendarHeader();
    }, 100);
    setTimeout(() => {
      syncScrollFromCalendarHeader();
    }, 500);
  });
});

onUnmounted(() => {
  if (cellsContainer.value) {
    cellsContainer.value.removeEventListener('scroll', handleScroll);
  }
  
  const calendarHeader = document.getElementById('calendar-dates-scroll');
  if (calendarHeader) {
    calendarHeader.removeEventListener('scroll', syncScrollFromCalendarHeader);
  }
  window.removeEventListener('pointerup', handleGlobalPointerUp);
  window.removeEventListener('click', handleDocumentClick);
  window.removeEventListener('keydown', handleKeyDown);
});

defineExpose({
  syncScroll: syncScrollFromCalendarHeader,
  cellsScrollWrapper,
});
</script>

<template>
  <div 
    ref="cellsScrollWrapper"
    class="calendar-cells-scroll-wrapper"
  >
    <div 
      ref="cellsContainer" 
      class="calendar-cells-container images-virtual-container"
    >
      <div class="cells-content" :style="{ width: blockWidth }">
      <template v-for="(item, index) in virtualItems" :key="`item-${index}`">
        <!-- Строка проекта - пустой блок -->
        <div 
          v-if="item.type === 'project'" 
          class="row-block project-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Загрузка батчей -->
        <div 
          v-else-if="item.type === 'loading-batches'" 
          class="row-block loading-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Пустые батчи -->
        <div 
          v-else-if="item.type === 'empty-batches'" 
          class="row-block empty-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Строка батча - пустой блок -->
        <div 
          v-else-if="item.type === 'batch'" 
          class="row-block batch-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Загрузка изображений -->
        <div 
          v-else-if="item.type === 'loading-images'" 
          class="row-block loading-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Пустые изображения -->
        <div 
          v-else-if="item.type === 'empty-images'" 
          class="row-block empty-row"
        >
          <div class="empty-block" :style="{ width: blockWidth }"></div>
        </div>
        
        <!-- Строка изображения - ячейки для каждой даты -->
        <div 
          v-else-if="item.type === 'image'" 
          class="row-block image-row"
        >
          <div class="cells-row">
            <div
              v-for="date in allDates"
              :key="`cell-${item.data.id}-${date}`"
              class="cell"
              :class="resolveCellClasses(item.data, date)"
              :style="resolveCellStyle(item.data, date)"
              :data-date="date"
              :data-image-id="item.data.id"
              :data-batch-id="item.data.batchId"
              :data-project-id="item.data.projectId"
              :title="getCellTooltip(item.data, date)"
              @click="handleCellClick($event, item.data, date)"
              @pointerdown="handleCellPointerDown($event, item, date)"
              @pointerenter="handleCellPointerEnter($event, item, date)"
              @contextmenu="handleCellContextMenu($event, item, date)"
            >
              <div class="cell-content">
                <i
                  v-if="!isWeekend(date) && getTaskForImageDate(item.data, date)?.completed"
                  class="pi pi-check cell-complete-icon"
                  title="Задача завершена"
                ></i>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Индикатор загрузки дополнительных элементов -->
      <div v-if="isLoadingMore" class="row-block loading-more-row">
        <div class="empty-block" :style="{ width: blockWidth }"></div>
      </div>

      <!-- Сообщение о конце списка -->
      <div v-if="!hasMore && props.projects.length > 0" class="row-block end-message-row">
        <div class="empty-block" :style="{ width: blockWidth }"></div>
      </div>
    </div>
    </div>
  </div>

  <div
    v-if="contextMenuState.visible"
    ref="contextMenuRef"
    class="calendar-context-menu"
    :style="{ top: `${contextMenuState.y}px`, left: `${contextMenuState.x}px` }"
    @contextmenu.prevent
  >
    <div class="context-summary">
      <div class="fw-semibold">{{ selectionSummary }}</div>
      <div class="text-muted small">
        {{ selectionTaskCount }} задач •
        <span v-if="selectedStatus">{{ selectedStatus.name }}</span>
        <span v-else>Статус не выбран</span>
      </div>
    </div>

    <button
      class="context-action"
      type="button"
      :disabled="!canFillSelection || isPerformingAction"
      @click="handleFillSelection"
    >
      <span>Закрасить выбранным статусом</span>
      <span
        v-if="selectedStatus"
        class="status-chip"
        :style="{ backgroundColor: selectedStatus.color, color: selectedStatus.textColor }"
      >
        {{ selectedStatus.name }}
      </span>
      <span v-else class="text-muted small">(выберите статус)</span>
    </button>

    <button
      class="context-action"
      type="button"
      :disabled="!canClearSelection || isPerformingAction"
      @click="handleClearSelection"
    >
      Очистить
    </button>

    <button
      class="context-action"
      type="button"
      :disabled="!selectionHasTasks || isPerformingAction"
      @click="handleToggleCompletion"
    >
      {{ toggleCompletionLabel }}
    </button>

    <button
      class="context-action"
      type="button"
      :disabled="!canAttachUsers || isPerformingAction"
      @click="openAttachUsersDialog"
    >
      Присоединить пользователей к задаче
    </button>
  </div>

  <Dialog
    v-model:visible="attachUsersDialog.visible"
    header="Присоединить пользователей"
    modal
    class="attach-users-dialog"
  >
    <div class="d-flex flex-column gap-3">
      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label">Фильтр по роли</label>
          <Dropdown
            v-model="attachUsersDialog.roleFilter"
            :options="roleFilterOptions"
            optionLabel="label"
            optionValue="value"
            class="w-100"
          />
        </div>
      </div>
      <div>
        <label class="form-label">Пользователи изображения</label>
        <MultiSelect
          v-model="attachUsersDialog.selectedUserIds"
          :options="filteredUserOptions"
          optionLabel="label"
          optionValue="value"
          display="chip"
          filter
          :filterFields="['label', 'roleLabel']"
          filterPlaceholder="Поиск по имени"
          placeholder="Выберите участников"
          class="w-100"
          :disabled="!filteredUserOptions.length"
        >
          <template #option="{ option }">
            <div class="user-option">
              <div class="user-option__name">{{ option.label }}</div>
              <div class="user-option__meta">
                <span class="user-option__role">{{ option.roleLabel }}</span>
              </div>
            </div>
          </template>
        </MultiSelect>
        <small
          v-if="!filteredUserOptions.length"
          class="text-muted d-block mt-2"
        >
          Пользователи ещё не загружены или не найдены.
        </small>
        <small class="text-muted d-block mt-2">
          Оставьте список пустым и нажмите «Применить», чтобы удалить всех участников из выбранных задач.
        </small>
      </div>
    </div>
    <template #footer>
      <Button label="Отмена" text @click="closeAttachUsersDialog" />
      <Button
        label="Применить"
        :disabled="attachUsersDialog.isSaving"
        :loading="attachUsersDialog.isSaving"
        @click="confirmAttachUsers"
      />
    </template>
  </Dialog>
</template>

<style scoped>
.calendar-cells-scroll-wrapper {
  flex: 1;
  overflow-y: hidden;
  overflow-x: hidden;
  position: relative;
  scrollbar-width: thin;
}

.calendar-cells-scroll-wrapper::-webkit-scrollbar {
  width: 6px;
}

.calendar-cells-scroll-wrapper::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.calendar-cells-scroll-wrapper::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 3px;
}

.calendar-cells-scroll-wrapper::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

.calendar-cells-container {
  overflow-x: auto;
  overflow-y: visible;
  position: relative;
  scrollbar-width: thin;
}

.calendar-cells-container::-webkit-scrollbar {
  height: 6px;
}

.calendar-cells-container::-webkit-scrollbar-track {
  background: #f1f1f1;
}

.calendar-cells-container::-webkit-scrollbar-thumb {
  background: #c1c1c1;
  border-radius: 3px;
}

.calendar-cells-container::-webkit-scrollbar-thumb:hover {
  background: #a8a8a8;
}

.cells-content {
  min-width: max-content;
}

.row-block {
  height: 6vh;
  min-height: 6vh;
  display: flex;
  align-items: stretch;
}

.empty-block {
  height: 100%;
  background: #f8f9fa;
  border-bottom: 1px solid #e9ecef;
}

.project-row .empty-block {
  background: #f0f4f8;
}

.batch-row .empty-block {
  background: #f5f7fa;
  border-left: 3px solid #e5e7eb;
}

.loading-row .empty-block,
.empty-row .empty-block {
  background: #fafbfc;
}

.loading-more-row .empty-block,
.end-message-row .empty-block {
  background: #f9fafb;
}

.image-row {
  background: #ffffff;
  border-left: 3px solid #d1d5db;
}

.cells-row {
  display: flex;
  height: 100%;
  min-width: max-content;
}

.cell {
  width: 4vw;
  min-width: 4vw;
  max-width: 4vw;
  height: 100%;
  box-sizing: border-box;
  border-right: 1px solid #e5e7eb;
  border-bottom: 1px solid #e5e7eb;
  background: #ffffff;
  cursor: pointer;
  transition: background-color 0.15s ease, border-color 0.15s ease;
  flex-shrink: 0;
}

.cell:hover {
  background: #e3f2fd;
  border-color: #90caf9;
}

.cell.weekend {
  background: #fafafa;
}

.cell.weekend:hover {
  background: #f0f0f0;
}

.cell.selected {
  position: relative;
  z-index: 1;
  box-shadow: 
    inset 0 0 0 2px #ff9800,
    0 0 8px rgba(255, 152, 0, 0.6);
}

.cell.has-task {
  background: var(--cell-color, #dbeafe);
  color: var(--cell-text-color, #0f172a);
  border-color: rgba(15, 23, 42, 0.12);
}

.cell.has-task.selected {
  position: relative;
  z-index: 2;
  box-shadow: 
    inset 0 0 0 2px #ff9800,
    0 0 12px rgba(255, 152, 0, 0.8),
    0 0 0 1px rgba(255, 152, 0, 0.4);
}

.cell.weekend-auto {
  background: var(--weekend-auto-color, rgba(148, 163, 184, 0.15));
  color: var(--weekend-auto-text-color, #475569);
  border-color: rgba(148, 163, 184, 0.25);
  opacity: 0.6;
}

.cell.completed {
  box-shadow: inset 0 0 0 2px rgba(16, 185, 129, 0.4);
}

.cell-content {
  width: 100%;
  height: 100%;
  display: flex;
  justify-content: flex-end;
  align-items: flex-start;
  padding: 6px;
  box-sizing: border-box;
}

.cell-complete-icon {
  font-size: 0.75rem;
  color: #10b981;
}

.calendar-context-menu {
  position: fixed;
  min-width: 260px;
  background: #ffffff;
  border: 1px solid rgba(15, 23, 42, 0.12);
  border-radius: 8px;
  box-shadow:
    0 20px 50px rgba(15, 23, 42, 0.15),
    0 8px 20px rgba(15, 23, 42, 0.08);
  padding: 12px;
  z-index: 50;
}

.context-summary {
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
  padding-bottom: 8px;
  margin-bottom: 8px;
}

.context-action {
  width: 100%;
  text-align: left;
  background: transparent;
  border: none;
  font-size: 0.9rem;
  padding: 8px 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  cursor: pointer;
  color: #0f172a;
}

.context-action:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.context-action:not(:disabled):hover {
  color: #2563eb;
}

.status-chip {
  font-size: 0.7rem;
  padding: 2px 10px;
  border-radius: 999px;
  white-space: nowrap;
}

.attach-users-dialog :deep(.p-dialog-content) {
  padding-top: 0;
}

.attach-users-dialog .form-label {
  font-size: 0.85rem;
  color: #475569;
  margin-bottom: 4px;
  display: inline-block;
}

.user-option {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.user-option__name {
  font-weight: 600;
}

.user-option__meta {
  font-size: 0.75rem;
  color: #64748b;
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}
</style>
