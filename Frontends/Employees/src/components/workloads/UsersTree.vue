<script setup>
const props = defineProps({
  rows: {
    type: Array,
    default: () => [],
  },
  expandedUsers: {
    type: Object,
    required: true,
  },
  expandedProjects: {
    type: Object,
    required: true,
  },
});

const emit = defineEmits(['toggle-user', 'toggle-project']);

const isExpanded = (row) => {
  if (row.type === 'user') {
    return props.expandedUsers.has(row.meta?.userId);
  }
  if (row.type === 'project') {
    return props.expandedProjects.has(row.meta?.projectKey);
  }
  return false;
};

const handleToggle = (row) => {
  if (row.type === 'user') {
    emit('toggle-user', row.meta?.userId);
    return;
  }
  if (row.type === 'project') {
    emit('toggle-project', row.meta?.projectKey);
  }
};

const resolveRowSubtitle = (row) => {
  if (row.type === 'user') {
    const parts = [];
    if (row.meta?.role) parts.push({ text: row.meta.role, type: 'role' });
    if (typeof row.meta?.projectsCount === 'number') {
      parts.push({ text: `${row.meta.projectsCount} проектов`, type: 'count' });
    }
    if (typeof row.meta?.tasksCount === 'number') {
      parts.push({ text: `${row.meta.tasksCount} задач`, type: 'count' });
    }
    return parts;
  }

  if (row.type === 'project') {
    const parts = [];
    if (row.meta?.clientName) {
      parts.push({ text: row.meta.clientName, type: 'client' });
    }
    if (typeof row.meta?.tasksCount === 'number') {
      parts.push({ text: `${row.meta.tasksCount} задач`, type: 'count' });
    }
    return parts;
  }

  if (row.type === 'task') {
    const parts = [];
    if (row.meta?.batchName) parts.push({ text: row.meta.batchName, type: 'batch' });
    if (row.meta?.imageName) parts.push({ text: row.meta.imageName, type: 'image' });
    if (row.meta?.statusName) parts.push({ text: row.meta.statusName, type: 'status' });
    return parts;
  }

  return [];
};

const resolveProjectPreview = (row) => {
  if (row.type !== 'user') return '';
  return row.meta?.projectPreview || '';
};
</script>

<template>
  <div class="users-tree">
    <div
      v-for="row in rows"
      :key="row.id"
      class="tree-row"
      :class="[`row-${row.type}`, { 'row-expanded': isExpanded(row) }]"
    >
      <div class="row-content" :style="{ paddingLeft: `${row.level * 16}px` }">
        <button
          v-if="row.canExpand"
          type="button"
          class="toggle-btn"
          :aria-label="isExpanded(row) ? 'Свернуть' : 'Развернуть'"
          @click="handleToggle(row)"
        >
          <i
            :class="[
              'pi',
              isExpanded(row) ? 'pi-chevron-down' : 'pi-chevron-right',
            ]"
            aria-hidden="true"
          ></i>
        </button>
        <div v-else class="toggle-placeholder"></div>

        <div class="labels">
          <div class="label-line">
            <span class="label-text">{{ row.label }}</span>
            <span
              v-if="row.type === 'task' && row.meta?.completed"
              class="badge badge-complete"
            >
              Завершено
            </span>
          </div>
          <div v-if="resolveRowSubtitle(row).length" class="subtitle">
            <span
              v-for="(part, idx) in resolveRowSubtitle(row)"
              :key="idx"
              class="chip"
              :class="`chip-${part.type}`"
            >
              {{ part.text }}
            </span>
          </div>
          <div
            v-if="row.type === 'user' && resolveProjectPreview(row)"
            class="project-preview"
          >
            {{ resolveProjectPreview(row) }}
          </div>
        </div>
      </div>
    </div>

    <div v-if="!rows.length" class="empty-state">
      <p>Нет данных для отображения. Попробуйте выбрать другой месяц.</p>
    </div>
  </div>
</template>

<style scoped>
.users-tree {
  display: flex;
  flex-direction: column;
}

.tree-row {
  border-bottom: 1px solid #e2e8f0;
  height: var(--row-height, 64px);
  display: flex;
  align-items: center;
  overflow: hidden;
  transition: background-color 0.15s ease;
  background-color: #ffffff;
}

.tree-row:hover {
  background-color: #f8fafc;
}

.row-project {
  background-color: #fafbfc;
}

.row-project:first-of-type {
  border-top: 2px solid #e2e8f0;
}

.row-task {
  background-color: #fcfcfd;
}

.row-task:last-of-type {
  border-bottom: 2px solid #e2e8f0;
}

.row-content {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  padding: 0 1.1rem;
  width: 100%;
  box-sizing: border-box;
  position: relative;
}

.row-user .row-content {
  padding: 0 1rem;
}

.row-user .row-content::before {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%);
  width: 4px;
  height: 50%;
  background: linear-gradient(180deg, #0ea5e9 0%, #06b6d4 100%);
  border-radius: 0 4px 4px 0;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.row-user.row-expanded .row-content::before {
  opacity: 1;
}

.toggle-btn {
  width: 32px;
  height: 32px;
  border: 1.5px solid #cbd5e1;
  border-radius: 8px;
  background: #ffffff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
  flex: 0 0 32px;
}

.toggle-btn:hover {
  background: #0ea5e9;
  border-color: #0ea5e9;
  box-shadow: 0 3px 6px rgba(14, 165, 233, 0.25);
}

.toggle-btn:hover i {
  color: #ffffff;
}

.toggle-btn:active {
  transform: scale(0.95);
}

.toggle-btn i {
  font-size: 1rem;
  color: #64748b;
  transition: color 0.15s ease;
  font-weight: bold;
}

.toggle-placeholder {
  width: 32px;
  flex: 0 0 32px;
}

.labels {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  overflow: hidden;
  flex: 1;
  min-width: 0;
}

.label-line {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  min-width: 0;
}

.label-text {
  font-weight: 600;
  color: #0f172a;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 1.05rem;
}

.row-user .label-text {
  font-size: 1.15rem;
  font-weight: 700;
  color: #0f172a;
}

.row-project .label-text {
  font-weight: 600;
  font-size: 1rem;
  color: #1e293b;
}

.row-task .label-text {
  font-weight: 500;
  font-size: 0.95rem;
  color: #334155;
}

.subtitle {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  align-items: center;
}

.chip {
  display: inline-flex;
  align-items: center;
  padding: 0.15rem 0.45rem;
  font-size: 0.75rem;
  font-weight: 500;
  border-radius: 3px;
  white-space: nowrap;
}

.chip-role {
  background-color: #e0f2fe;
  color: #0369a1;
}

.chip-count {
  background-color: #f1f5f9;
  color: #475569;
}

.chip-client {
  background-color: #fef3c7;
  color: #92400e;
}

.chip-batch {
  background-color: #e9d5ff;
  color: #6b21a8;
}

.chip-image {
  background-color: #ddd6fe;
  color: #5b21b6;
}

.chip-status {
  background-color: #d1fae5;
  color: #065f46;
}

.project-preview {
  font-size: 0.85rem;
  color: #0ea5e9;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-weight: 500;
  font-style: italic;
}

.badge {
  font-size: 0.7rem;
  padding: 0.25rem 0.7rem;
  border-radius: 6px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 700;
}

.badge-complete {
  background: #10b981;
  color: #ffffff;
  box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);
}

.empty-state {
  padding: 3rem 2rem;
  text-align: center;
  color: #94a3b8;
}

.empty-state p {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 500;
}
</style>

