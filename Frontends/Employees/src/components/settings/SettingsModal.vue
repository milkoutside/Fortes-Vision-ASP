<script setup>
import { computed } from 'vue';
import UserManager from './UserManager.vue';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(['update:visible']);

const internalVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});
</script>

<template>
  <Dialog
    v-model:visible="internalVisible"
    header="Управление пользователями"
    modal
    :draggable="false"
    blockScroll
    dismissableMask
    class="settings-dialog"
    :style="{ width: '100vw', maxWidth: '100vw', height: '100vh', maxHeight: '100vh', margin: 0, top: 0 }"
    :contentStyle="{ height: 'calc(100vh - 80px)', overflow: 'auto', padding: 0 }"
  >
    <UserManager />
  </Dialog>
</template>

<style scoped>
.settings-dialog :deep(.p-dialog) {
  width: 100vw !important;
  height: 100vh !important;
  max-width: 100vw !important;
  max-height: 100vh !important;
  margin: 0 !important;
  top: 0 !important;
  left: 0 !important;
  border-radius: 0 !important;
}

.settings-dialog :deep(.p-dialog-content) {
  padding: 0 !important;
}

.settings-dialog :deep(.p-dialog-header) {
  font-size: 1.25rem;
  font-weight: 600;
}
</style>

