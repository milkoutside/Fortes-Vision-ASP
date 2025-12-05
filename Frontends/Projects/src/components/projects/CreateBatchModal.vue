<script setup>
import { computed, reactive, ref, watch } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import { createBatch, createBatchCalculator } from '../../repositories/batchesRepository';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
  project: {
    type: Object,
    default: null,
  },
});

const emit = defineEmits(['update:visible', 'batch-created']);

const internalVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const store = useStore();
const toast = useToast();

const form = reactive({
  name: '',
  statusDurations: [],
});

const isSubmitting = ref(false);
const draggedIndex = ref(null);

const statuses = computed(() => store.state.statuses.items);

const resetForm = () => {
  form.name = '';
  form.statusDurations = [];
};

const addStatusDuration = () => {
  form.statusDurations.push({
    status: null,
    duration: 1,
  });
};

const removeStatusDuration = (index) => {
  form.statusDurations.splice(index, 1);
};

const moveStatusDuration = (fromIndex, toIndex) => {
  const item = form.statusDurations.splice(fromIndex, 1)[0];
  form.statusDurations.splice(toIndex, 0, item);
};

const handleDragStart = (index) => {
  draggedIndex.value = index;
};

const handleDragOver = (event, index) => {
  event.preventDefault();
  if (draggedIndex.value === null || draggedIndex.value === index) return;
  
  const draggedItem = form.statusDurations[draggedIndex.value];
  const targetItem = form.statusDurations[index];
  
  if (draggedItem && targetItem) {
    moveStatusDuration(draggedIndex.value, index);
    draggedIndex.value = index;
  }
};

const handleDragEnd = () => {
  draggedIndex.value = null;
};

const getStatusName = (statusId) => {
  const status = statuses.value.find(s => s.id === statusId);
  return status ? status.name : 'Не выбран';
};

const isFormValid = computed(() => {
  const hasName = !!form.name.trim();
  if (!hasName) {
    return false;
  }

  if (form.statusDurations.length === 0) {
    return true;
  }

  return form.statusDurations.every(
    (sd) => sd.status && Number(sd.duration) > 0,
  );
});

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      resetForm();
      // Загружаем статусы, если они еще не загружены
      if (!store.state.statuses.hasLoaded) {
        store.dispatch('statuses/fetchAll');
      }
    }
  },
);

const handleSubmit = async () => {
  if (!form.name.trim()) {
    toast.add({
      severity: 'warn',
      summary: 'Название обязательно',
      detail: 'Введите название батча.',
      life: 4000,
    });
    return;
  }

  const hasCalculator = form.statusDurations.length > 0;
  if (hasCalculator) {
    const invalidStatus = form.statusDurations.find(
      (sd) => !sd.status || Number(sd.duration) <= 0,
    );
    if (invalidStatus) {
      toast.add({
        severity: 'warn',
        summary: 'Проверьте данные',
        detail:
          'Все статусы должны быть выбраны, а длительность должна быть больше 0.',
        life: 4000,
      });
      return;
    }
  }

  if (!props.project?.id) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: 'Проект не выбран.',
      life: 5000,
    });
    return;
  }

  isSubmitting.value = true;
  try {
    // Создаем батч
    const batch = await createBatch(props.project.id, {
      name: form.name.trim(),
      projectId: props.project.id,
    });

    if (hasCalculator) {
      await createBatchCalculator({
        batchId: batch.id,
        projectId: props.project.id,
        statusDurations: form.statusDurations.map((sd) => ({
          status: sd.status,
          duration: sd.duration,
        })),
      });
    }

    toast.add({
      severity: 'success',
      summary: 'Батч создан',
      detail: `«${batch.name}» успешно создан.`,
      life: 3000,
    });
    
    emit('batch-created', props.project.id);
    internalVisible.value = false;
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка создания батча',
      detail: error.message ?? 'Попробуйте снова.',
      life: 6000,
    });
  } finally {
    isSubmitting.value = false;
  }
};
</script>

<template>
  <Dialog
    v-model:visible="internalVisible"
    header="Создать батч"
    modal
    dismissableMask
    :draggable="false"
    blockScroll
    class="create-batch-dialog"
    :style="{ width: '720px', maxWidth: '95vw' }"
  >
    <form class="d-flex flex-column gap-4" @submit.prevent="handleSubmit">
      <div>
        <label class="form-label fw-semibold">Название батча</label>
        <InputText
          v-model="form.name"
          class="w-100"
          placeholder="Введите название батча"
          autocomplete="off"
        />
      </div>

      <div>
        <div class="d-flex align-items-center justify-content-between mb-3">
          <label class="form-label fw-semibold mb-0">Batch Calculator (опционально)</label>
          <Button
            type="button"
            icon="pi pi-plus"
            label="Добавить статус"
            size="small"
            @click="addStatusDuration"
          />
        </div>

        <div v-if="form.statusDurations.length === 0" class="text-center py-4 text-muted border rounded">
          <i class="pi pi-info-circle d-block mb-2"></i>
          <span>Вы пока не добавили статусы. Это опционально.</span>
        </div>

        <div v-else class="d-flex flex-column gap-2">
          <div
            v-for="(statusDuration, index) in form.statusDurations"
            :key="index"
            class="batch-calculator-item p-3 border rounded d-flex align-items-center gap-3"
            :class="{ 'dragging': draggedIndex === index }"
            draggable="true"
            @dragstart="handleDragStart(index)"
            @dragover="handleDragOver($event, index)"
            @dragend="handleDragEnd"
          >
            <div class="drag-handle cursor-move">
              <i class="pi pi-bars text-muted"></i>
            </div>
            
            <div class="flex-grow-1 d-flex gap-3">
              <div class="flex-grow-1">
                <label class="form-label small mb-1">Статус</label>
                <Select
                  v-model="statusDuration.status"
                  :options="statuses"
                  optionLabel="name"
                  optionValue="id"
                  placeholder="Выберите статус"
                  class="w-100"
                />
              </div>
              
              <div style="width: 150px;">
                <label class="form-label small mb-1">Длительность</label>
                <InputNumber
                  v-model="statusDuration.duration"
                  :min="1"
                  :max="9999"
                  class="w-100"
                  placeholder="Дни"
                />
              </div>
            </div>
            
            <Button
              type="button"
              icon="pi pi-trash"
              severity="danger"
              text
              rounded
              @click="removeStatusDuration(index)"
            />
          </div>
        </div>
      </div>

      <div class="d-flex justify-content-end gap-2">
        <Button type="button" label="Отмена" severity="secondary" class="px-4" @click="internalVisible = false" />
        <Button
          type="submit"
          label="Создать"
          class="px-4"
          :disabled="!isFormValid || isSubmitting"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.create-batch-dialog :deep(.p-dialog-content) {
  padding-top: 0.75rem;
}

.form-label {
  color: #475569;
}

.batch-calculator-item {
  background-color: #f8fafc;
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.batch-calculator-item:hover {
  background-color: #f1f5f9;
}

.batch-calculator-item.dragging {
  opacity: 0.5;
  transform: scale(0.98);
}

.drag-handle {
  cursor: grab;
}

.drag-handle:active {
  cursor: grabbing;
}

.cursor-move {
  cursor: move;
}

form :deep(.p-inputtext),
form :deep(.p-select),
form :deep(.p-inputnumber) {
  width: 100%;
}
</style>

