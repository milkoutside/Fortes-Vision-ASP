<script setup>
import StatusPicker from './status/StatusPicker.vue';

const emit = defineEmits(['open-settings', 'open-create-project']);

const PROJECTS_APP_URL = import.meta.env.VITE_PROJECTS_APP_URL ?? 'http://135.181.14.10:5173';
const EMPLOYEES_APP_URL = import.meta.env.VITE_EMPLOYEES_APP_URL ?? 'http://135.181.14.10:5174';

const handleSettingsClick = () => {
  emit('open-settings');
};

const handleCreateProjectClick = () => {
  emit('open-create-project');
};

const navigateToProjects = () => {
  window.location.href = PROJECTS_APP_URL;
};

const navigateToEmployees = () => {
  window.location.href = EMPLOYEES_APP_URL;
};
</script>

<template>
  <header class="app-header d-flex align-items-center gap-4 px-4 py-3 shadow-sm bg-white">
    <div class="d-flex align-items-center gap-4">
      <a
        class="app-link text-decoration-none fw-semibold active"
        :href="PROJECTS_APP_URL"
        rel="noopener"
        @click.prevent="navigateToProjects"
      >
        Projects
      </a>
      <a
        class="app-link text-decoration-none fw-semibold"
        :href="EMPLOYEES_APP_URL"
        rel="noopener"
        @click.prevent="navigateToEmployees"
      >
        Employees
      </a>
    </div>

    <Button
      label="Create Project"
      icon="pi pi-plus"
      severity="primary"
      class="rounded-pill px-3 create-project-btn"
      @click="handleCreateProjectClick"
    />

    <div class="app-header__center flex-grow-1 d-flex align-items-center justify-content-center">
      <StatusPicker class="w-100" />
    </div>

    <button
      type="button"
      class="settings-btn d-inline-flex align-items-center justify-content-center rounded-circle border-0"
      aria-label="Settings"
      @click="handleSettingsClick"
    >
      <i class="pi pi-cog fs-5"></i>
    </button>
  </header>
</template>

<style scoped>
.app-header {
  min-height: 72px;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
}

.app-link {
  color: #1f2937;
  transition: color 0.15s ease;
}

.app-link.active {
  color: #0d6efd;
}

.app-link:hover {
  color: #0d6efd;
}

.settings-btn {
  width: 48px;
  height: 48px;
  background-color: #f3f4f6;
  color: #1f2937;
  transition: background-color 0.2s ease, color 0.2s ease;
  flex: 0 0 48px;
}

.settings-btn:hover {
  background-color: #e5e7eb;
  color: #0d6efd;
}

.app-header__center {
  min-width: 220px;
  padding: 0 0.5rem;
  flex: 0 1 clamp(360px, 60vw, 960px);
  margin-left: auto;
  margin-right: auto;
}

.create-project-btn {
  white-space: nowrap;
  flex: 0 0 auto;
}

</style>

