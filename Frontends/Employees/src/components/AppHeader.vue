<script setup>
const emit = defineEmits(['open-settings', 'scroll-to-today']);

const PROJECTS_APP_URL = import.meta.env.VITE_PROJECTS_APP_URL ?? 'http://135.181.14.10:5173';
const EMPLOYEES_APP_URL = import.meta.env.VITE_EMPLOYEES_APP_URL ?? 'http://135.181.14.10:5174';

const handleSettingsClick = () => {
  emit('open-settings');
};

const handleTodayClick = () => {
  emit('scroll-to-today');
};

const navigateToProjects = () => {
  window.location.href = PROJECTS_APP_URL;
};

const navigateToEmployees = () => {
  window.location.href = EMPLOYEES_APP_URL;
};
</script>

<template>
  <header class="app-header">
    <div class="header-links">
      <a
        class="app-link text-decoration-none fw-semibold"
        :href="PROJECTS_APP_URL"
        rel="noopener"
        @click.prevent="navigateToProjects"
      >
        Projects
      </a>
      <a
        class="app-link text-decoration-none fw-semibold active"
        :href="EMPLOYEES_APP_URL"
        rel="noopener"
        @click.prevent="navigateToEmployees"
      >
        Employees
      </a>
    </div>

    <div class="spacer"></div>

    <button
      type="button"
      class="today-btn"
      aria-label="Today"
      @click="handleTodayClick"
    >
      Today
    </button>

    <button
      type="button"
      class="settings-btn"
      aria-label="Settings"
      @click="handleSettingsClick"
    >
      <i class="pi pi-cog"></i>
    </button>
  </header>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem 1.5rem;
  min-height: 72px;
  background: #ffffff;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  flex-shrink: 0;
}

.header-links {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.spacer {
  flex: 1;
}

.app-link {
  color: #64748b;
  transition: color 0.15s ease;
  font-size: 1rem;
}

.app-link:hover {
  color: #0ea5e9;
}

.app-link.active {
  color: #0f172a;
  font-weight: 700;
}

.today-btn {
  padding: 0.5rem 1rem;
  background-color: #0ea5e9;
  color: #ffffff;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
  font-size: 0.9rem;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.today-btn:hover {
  background-color: #38bdf8;
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(14, 165, 233, 0.3);
}

.today-btn:active {
  transform: translateY(0);
}

.settings-btn {
  width: 48px;
  height: 48px;
  background-color: #f3f4f6;
  color: #1f2937;
  border: none;
  border-radius: 50%;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  flex: 0 0 48px;
}

.settings-btn:hover {
  background-color: #e5e7eb;
  color: #0ea5e9;
  transform: rotate(45deg);
}

.settings-btn i {
  font-size: 1.25rem;
}
</style>

