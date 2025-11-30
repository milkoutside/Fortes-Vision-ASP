import {
  fetchTasks,
  bulkCreateTasks,
  bulkUpdateTasks,
  bulkDeleteTasks,
} from '../../repositories/tasksRepository';

const buildImageKey = ({ projectId, batchId, imageId }) =>
  `${projectId}:${batchId}:${imageId}`;

const buildCellKey = ({ projectId, batchId, imageId, date }) =>
  `${projectId}:${batchId}:${imageId}:${date}`;

const toDateParts = (value) => {
  const [year, month, day] = value.split('-').map(Number);
  return { year, month: month - 1, day };
};

const parseDate = (value) => {
  if (value instanceof Date) {
    return new Date(
      Date.UTC(value.getUTCFullYear(), value.getUTCMonth(), value.getUTCDate()),
    );
  }
  if (typeof value === 'string') {
    const { year, month, day } = toDateParts(value);
    return new Date(Date.UTC(year, month, day));
  }
  return new Date(value);
};

const formatDate = (date) => {
  const d = parseDate(date);
  const year = d.getUTCFullYear();
  const month = String(d.getUTCMonth() + 1).padStart(2, '0');
  const day = String(d.getUTCDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const addDays = (date, amount) => {
  const d = parseDate(date);
  d.setUTCDate(d.getUTCDate() + amount);
  return formatDate(d);
};

const compareDates = (a, b) => {
  if (a === b) return 0;
  return a < b ? -1 : 1;
};

const isWeekend = (value) => {
  const date = parseDate(value);
  const day = date.getUTCDay();
  return day === 0 || day === 6;
};

const nextBusinessDay = (value) => {
  let cursor = addDays(value, 1);
  while (isWeekend(cursor)) {
    cursor = addDays(cursor, 1);
  }
  return cursor;
};

const previousBusinessDay = (value) => {
  let cursor = addDays(value, -1);
  while (isWeekend(cursor)) {
    cursor = addDays(cursor, -1);
  }
  return cursor;
};

const enumerateBusinessDates = (start, end) => {
  const dates = [];
  let cursor = start;
  while (compareDates(cursor, end) <= 0) {
    if (!isWeekend(cursor)) {
      dates.push(cursor);
    }
    cursor = addDays(cursor, 1);
  }
  return dates;
};

// Перечисляет ВСЕ даты включая выходные
// ВНИМАНИЕ: используется только для специальных случаев
// В основном используйте enumerateBusinessDates
const enumerateDates = (start, end) => {
  const dates = [];
  let cursor = start;
  while (compareDates(cursor, end) <= 0) {
    dates.push(cursor);
    cursor = addDays(cursor, 1);
  }
  return dates;
};

const getTaskRange = (task) => {
  const start = task.startDate;
  const end = task.endDate ?? task.startDate;
  return { start, end };
};

const createImageCollection = () => ({
  tasks: [],
  byDate: {},
  tasksById: {},
  isLoading: false,
  isLoaded: false,
  error: null,
});

const buildTaskIndexes = (tasks = []) => {
  const sorted = [...tasks].sort((a, b) => compareDates(a.startDate, b.startDate));
  const byDate = {};
  const tasksById = {};
  sorted.forEach((task) => {
    const range = getTaskRange(task);
    tasksById[task.id] = { ...task, startDate: range.start, endDate: range.end };
    // ВАЖНО: Используем enumerateDates чтобы задачи отображались на ВСЕХ днях включая выходные
    // Если задача с startDate=Пт и endDate=Пн, то она будет видна на Пт, Сб, Вс, Пн
    enumerateDates(range.start, range.end).forEach((date) => {
      byDate[date] = { taskId: task.id };
    });
  });
  return {
    tasks: sorted,
    byDate,
    tasksById,
  };
};

const uniqueCells = (cells) => {
  const map = new Map();
  (cells ?? []).forEach((cell) => {
    if (
      cell &&
      typeof cell.projectId === 'number' &&
      typeof cell.batchId === 'number' &&
      typeof cell.imageId === 'number' &&
      cell.date
    ) {
      map.set(buildCellKey(cell), cell);
    }
  });
  return Array.from(map.values());
};

const groupCellsByImage = (cells) => {
  const groups = uniqueCells(cells).reduce((acc, cell) => {
    const scope = {
      projectId: cell.projectId,
      batchId: cell.batchId,
      imageId: cell.imageId,
    };
    const imageKey = buildImageKey(scope);
    if (!acc[imageKey]) {
      acc[imageKey] = {
        key: imageKey,
        scope,
        cells: [],
      };
    }
    acc[imageKey].cells.push(cell);
    return acc;
  }, {});
  return Object.values(groups);
};

const buildSegments = (cells) => {
  // ВАЖНО: НЕ фильтруем выходные - пользователь может их явно выделить и закрасить!
  // Группируем смежные ячейки:
  // 1. Последовательные дни (включая выходные) = один сегмент
  // 2. Рабочие дни через выходные (Пт -> Пн) = один сегмент
  // 3. Разрыв больше чем выходные = новый сегмент
  const sorted = [...cells].sort((a, b) => compareDates(a.date, b.date));
  const segments = [];
  let current = null;
  
  sorted.forEach((cell) => {
    if (!current) {
      current = { startDate: cell.date, endDate: cell.date };
      return;
    }
    
    // Проверяем, является ли текущая ячейка смежной с предыдущей
    const prevDate = current.endDate;
    const nextDay = addDays(prevDate, 1);
    
    // Если это следующий день (любой, включая выходной) - продолжаем сегмент
    if (cell.date === nextDay) {
      current.endDate = cell.date;
      return;
    }
    
    // Если не следующий день - проверяем, может это следующий рабочий день через выходные?
    const nextBusinessDayDate = nextBusinessDay(prevDate);
    if (cell.date === nextBusinessDayDate) {
      // Это следующий рабочий день после выходных - продолжаем сегмент
      current.endDate = cell.date;
      return;
    }
    
    // Иначе - начинаем новый сегмент
    segments.push(current);
    current = { startDate: cell.date, endDate: cell.date };
  });
  
  if (current) {
    segments.push(current);
  }
  return segments;
};

const ensureImageEntry = (state, imageKey) => {
  if (!state.collections[imageKey]) {
    state.collections = {
      ...state.collections,
      [imageKey]: createImageCollection(),
    };
  }
  return state.collections[imageKey];
};

const findTasksOverlapping = (entry, startDate, endDate) => {
  if (!entry?.tasks?.length) return [];
  return entry.tasks.filter((task) => {
    const range = getTaskRange(task);
    const startsBeforeEnd = compareDates(range.start, endDate) <= 0;
    const endsAfterStart = compareDates(range.end, startDate) >= 0;
    return startsBeforeEnd && endsAfterStart;
  });
};

const subtractRangeFromTask = (task, start, end) => {
  const range = getTaskRange(task);
  const segments = [];
  if (compareDates(start, range.start) > 0) {
    segments.push({
      startDate: range.start,
      endDate: addDays(start, -1),
    });
  }
  if (compareDates(end, range.end) < 0) {
    segments.push({
      startDate: addDays(end, 1),
      endDate: range.end,
    });
  }
  return segments.filter(
    (segment) => compareDates(segment.startDate, segment.endDate) <= 0,
  );
};

const getTaskAtDate = (entry, date) => {
  if (!entry) return null;
  const meta = entry.byDate[date];
  if (!meta) return null;
  return entry.tasksById[meta.taskId] ?? null;
};

const mergeUserLists = (...lists) => {
  const map = new Map();
  lists.flat().forEach((user) => {
    if (!user) return;
    const id = user.userId ?? user.id;
    if (!id) return;
    if (!map.has(id)) {
      map.set(id, {
        id,
        name: user.name ?? user.label ?? '',
        role: user.role ?? 'artist',
      });
    }
  });
  return Array.from(map.values());
};

const toUserPayload = (users = []) =>
  users
    .map((user) => {
      const id = user.userId ?? user.id;
      if (!id) return null;
      return {
        userId: id,
        role: user.role ?? 'artist',
      };
    })
    .filter(Boolean);

const buildPayloadFromTask = (task, range) => {
  const payload = {
    statusId: task.status.id,
    startDate: range.startDate,
    completed: task.completed,
  };
  if (compareDates(range.startDate, range.endDate) !== 0) {
    payload.endDate = range.endDate;
    payload.dueDate = range.endDate;
  }
  if (task.users?.length) {
    payload.users = toUserPayload(task.users);
  }
  return payload;
};

const buildPayloadForStatus = (statusId, range, users = [], completed = false) => {
  const payload = {
    statusId,
    startDate: range.startDate,
    completed,
  };
  if (compareDates(range.startDate, range.endDate) !== 0) {
    payload.endDate = range.endDate;
    payload.dueDate = range.endDate;
  }
  if (users.length) {
    payload.users = toUserPayload(users);
  }
  return payload;
};

const runOperations = async (scope, { deleteIds, creates }) => {
  // Удаляем задачи только если есть что удалять
  if (deleteIds && deleteIds.size > 0) {
    try {
      await bulkDeleteTasks(scope, Array.from(deleteIds));
    } catch (error) {
      // Игнорируем ошибки удаления - задачи могут быть уже удалены
      console.warn('Failed to delete some tasks:', error);
    }
  }
  // Создаем задачи только если есть что создавать
  if (creates && creates.length > 0) {
    await bulkCreateTasks(scope, creates);
  }
};

const initialState = () => ({
  collections: {},
});

const mutations = {
  setImageLoading(state, { imageKey, isLoading }) {
    const entry = ensureImageEntry(state, imageKey);
    state.collections = {
      ...state.collections,
      [imageKey]: {
        ...entry,
        isLoading,
      },
    };
  },
  setImageError(state, { imageKey, error }) {
    const entry = ensureImageEntry(state, imageKey);
    state.collections = {
      ...state.collections,
      [imageKey]: {
        ...entry,
        error,
      },
    };
  },
  setImageTasks(state, { imageKey, tasks }) {
    const entry = ensureImageEntry(state, imageKey);
    const indexes = buildTaskIndexes(tasks ?? []);
    state.collections = {
      ...state.collections,
      [imageKey]: {
        ...entry,
        tasks: indexes.tasks,
        byDate: indexes.byDate,
        tasksById: indexes.tasksById,
        isLoaded: true,
        error: null,
      },
    };
  },
  resetState(state) {
    Object.assign(state, initialState());
  },
};

const actions = {
  async fetchForImage({ commit }, scope) {
    const imageKey = buildImageKey(scope);
    commit('setImageLoading', { imageKey, isLoading: true });
    commit('setImageError', { imageKey, error: null });
    try {
      const tasks = await fetchTasks(scope);
      commit('setImageTasks', { imageKey, tasks });
      return tasks;
    } catch (error) {
      commit('setImageError', { imageKey, error });
      throw error;
    } finally {
      commit('setImageLoading', { imageKey, isLoading: false });
    }
  },
  async ensureForImage({ state, dispatch }, scope) {
    const imageKey = buildImageKey(scope);
    const entry = state.collections[imageKey];
    if (entry?.isLoaded || entry?.isLoading) {
      return;
    }
    await dispatch('fetchForImage', scope);
  },
  async applyStatusToCells({ state, dispatch }, { cells, statusId }) {
    if (!statusId) {
      throw new Error('Статус не выбран.');
    }
    const groups = groupCellsByImage(cells);
    let created = 0;
    let updated = 0;
    for (const group of groups) {
      await dispatch('ensureForImage', group.scope);
      const imageKey = group.key;
      const entry = state.collections[imageKey];
      if (!entry) continue;
      
      // Отслеживаем уже удаленные задачи во всех сегментах этого image
      const globalDeletedIds = new Set();
      
      const segments = buildSegments(group.cells);
      for (const segment of segments) {
        const deleteIds = new Set();
        const creates = [];
        const overlapping = findTasksOverlapping(
          entry,
          segment.startDate,
          segment.endDate,
        );
        overlapping.forEach((task) => {
          if (!globalDeletedIds.has(task.id)) {
            deleteIds.add(task.id);
            globalDeletedIds.add(task.id);
          }
          const leftovers = subtractRangeFromTask(
            task,
            segment.startDate,
            segment.endDate,
          );
          leftovers.forEach((range) => {
            creates.push(buildPayloadFromTask(task, range));
          });
        });

        let finalStart = segment.startDate;
        let finalEnd = segment.endDate;
        let mergedUsers = mergeUserLists(
          overlapping.flatMap((task) => task.users ?? []),
        );

        // Проверяем соседа слева
        // 1. Сначала проверяем предыдущий день (может быть выходной)
        // 2. Если это выходной без задачи, проверяем предыдущий рабочий день
        const prevDay = addDays(segment.startDate, -1);
        let leftNeighbor = getTaskAtDate(entry, prevDay);
        
        // Если на предыдущем дне нет задачи и это выходной, проверим предыдущий рабочий день
        if (!leftNeighbor && isWeekend(prevDay)) {
          const leftBusinessDay = previousBusinessDay(segment.startDate);
          leftNeighbor = getTaskAtDate(entry, leftBusinessDay);
        }
        
        if (
          leftNeighbor &&
          leftNeighbor.status.id === statusId &&
          !deleteIds.has(leftNeighbor.id) &&
          !globalDeletedIds.has(leftNeighbor.id)
        ) {
          deleteIds.add(leftNeighbor.id);
          globalDeletedIds.add(leftNeighbor.id);
          const range = getTaskRange(leftNeighbor);
          finalStart = compareDates(range.start, finalStart) < 0 ? range.start : finalStart;
          mergedUsers = mergeUserLists(mergedUsers, leftNeighbor.users);
          updated += 1;
        }

        // Проверяем соседа справа
        // 1. Сначала проверяем следующий день (может быть выходной)
        // 2. Если это выходной без задачи, проверяем следующий рабочий день
        const nextDay = addDays(segment.endDate, 1);
        let rightNeighbor = getTaskAtDate(entry, nextDay);
        
        // Если на следующем дне нет задачи и это выходной, проверим следующий рабочий день
        if (!rightNeighbor && isWeekend(nextDay)) {
          const rightBusinessDay = nextBusinessDay(segment.endDate);
          rightNeighbor = getTaskAtDate(entry, rightBusinessDay);
        }
        
        if (
          rightNeighbor &&
          rightNeighbor.status.id === statusId &&
          !deleteIds.has(rightNeighbor.id) &&
          !globalDeletedIds.has(rightNeighbor.id)
        ) {
          deleteIds.add(rightNeighbor.id);
          globalDeletedIds.add(rightNeighbor.id);
          const range = getTaskRange(rightNeighbor);
          finalEnd = compareDates(range.end, finalEnd) > 0 ? range.end : finalEnd;
          mergedUsers = mergeUserLists(mergedUsers, rightNeighbor.users);
          updated += 1;
        }

        overlapping.forEach(() => {
          updated += 1;
        });

        const referenceTask = leftNeighbor ?? rightNeighbor ?? overlapping[0];
        const mergedCompleted = referenceTask ? referenceTask.completed : false;

        creates.push(
          buildPayloadForStatus(
            statusId,
            { startDate: finalStart, endDate: finalEnd },
            mergedUsers,
            mergedCompleted,
          ),
        );
        created += 1;

        // Отправляем операции только если есть что обрабатывать
        if (deleteIds.size > 0 || creates.length > 0) {
          await runOperations(group.scope, { deleteIds, creates });
          // Обновляем entry после каждого сегмента для корректной работы getTaskAtDate
          await dispatch('fetchForImage', group.scope);
        }
      }
    }
    return { created, updated };
  },
  async clearCells({ state, dispatch }, { cells }) {
    const groups = groupCellsByImage(cells);
    let removed = 0;
    for (const group of groups) {
      await dispatch('ensureForImage', group.scope);
      const imageKey = group.key;
      const entry = state.collections[imageKey];
      if (!entry) continue;
      const segments = buildSegments(group.cells);
      for (const segment of segments) {
        const deleteIds = new Set();
        const creates = [];
        const overlapping = findTasksOverlapping(
          entry,
          segment.startDate,
          segment.endDate,
        );
        overlapping.forEach((task) => {
          deleteIds.add(task.id);
          const range = getTaskRange(task);
          const overlapStart =
            compareDates(range.start, segment.startDate) >= 0 ? range.start : segment.startDate;
          const overlapEnd =
            compareDates(range.end, segment.endDate) <= 0 ? range.end : segment.endDate;
          // ВАЖНО: Используем enumerateDates чтобы считать ВСЕ дни включая выходные
          removed += enumerateDates(overlapStart, overlapEnd).length;
          const leftovers = subtractRangeFromTask(
            task,
            segment.startDate,
            segment.endDate,
          );
          leftovers.forEach((range) => {
            creates.push(buildPayloadFromTask(task, range));
          });
        });
        if (deleteIds.size || creates.length) {
          await runOperations(group.scope, { deleteIds, creates });
          await dispatch('fetchForImage', group.scope);
        }
      }
    }
    return { removed };
  },
  async completeCells({ state, dispatch }, { cells, completed = true }) {
    const groups = groupCellsByImage(cells);
    let updated = 0;
    for (const group of groups) {
      await dispatch('ensureForImage', group.scope);
      const imageKey = group.key;
      const entry = state.collections[imageKey];
      if (!entry) continue;
      const taskIds = new Set();
      group.cells.forEach((cell) => {
        const task = getTaskAtDate(entry, cell.date);
        if (task) {
          taskIds.add(task.id);
        }
      });
      if (!taskIds.size) continue;
      const payload = Array.from(taskIds).map((taskId) => ({
        taskId,
        completed,
      }));
      await bulkUpdateTasks(group.scope, payload);
      await dispatch('fetchForImage', group.scope);
      updated += payload.length;
    }
    return { updated };
  },
  async attachUsersToCells({ state, dispatch }, { cells, users }) {
    const groups = groupCellsByImage(cells);
    let updated = 0;
    for (const group of groups) {
      await dispatch('ensureForImage', group.scope);
      const imageKey = group.key;
      const entry = state.collections[imageKey];
      if (!entry) continue;
      const taskIds = new Set();
      group.cells.forEach((cell) => {
        const task = getTaskAtDate(entry, cell.date);
        if (task) {
          taskIds.add(task.id);
        }
      });
      if (!taskIds.size) continue;
      const payloadUsers = (users ?? [])
        .map((user) =>
          typeof user === 'object'
            ? {
                userId: user.userId ?? user.id ?? user.value,
                role: user.role ?? 'artist',
              }
            : { userId: user, role: 'artist' },
        )
        .filter(Boolean);
      const payload = Array.from(taskIds).map((taskId) => ({
        taskId,
        users: payloadUsers,
      }));
      await bulkUpdateTasks(group.scope, payload);
      await dispatch('fetchForImage', group.scope);
      updated += payload.length;
    }
    return { updated };
  },
};

export default {
  namespaced: true,
  state: initialState,
  mutations,
  actions,
};
