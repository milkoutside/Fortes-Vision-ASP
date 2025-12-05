<script setup>
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import DatePicker from 'primevue/datepicker';
import MultiSelect from 'primevue/multiselect';
import ToggleSwitch from 'primevue/toggleswitch';

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

const emit = defineEmits(['update:visible']);

const internalVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const store = useStore();
const toast = useToast();

const createDefaultForm = () => ({
  name: '',
  isActive: true,
  startDate: null,
  endDate: null,
  clientName: '',
  deadlineType: 'soft',
  projectManagers: [],
});

const form = reactive(createDefaultForm());
const isSubmitting = ref(false);
const managerOptions = ref([]);
const isManagersLoading = ref(false);
let managerFilterTimer;

const deadlineOptions = [
  { label: 'Гибкий (soft)', value: 'soft' },
  { label: 'Жёсткий (hard)', value: 'hard' },
];

const dateError = computed(() => {
  if (form.startDate && form.endDate && form.endDate < form.startDate) {
    return 'Дата окончания не может быть раньше даты начала';
  }
  return '';
});

const resetForm = () => {
  Object.assign(form, createDefaultForm());
};

const loadProjectData = () => {
  if (!props.project) {
    resetForm();
    return;
  }

  form.name = props.project.name || '';
  form.isActive = props.project.isActive ?? true;
  form.clientName = props.project.clientName || '';
  form.deadlineType = props.project.deadlineType || 'soft';
  
  if (props.project.startDate) {
    const [year, month, day] = props.project.startDate.split('-');
    form.startDate = new Date(year, month - 1, day);
  } else {
    form.startDate = null;
  }
  
  if (props.project.endDate) {
    const [year, month, day] = props.project.endDate.split('-');
    form.endDate = new Date(year, month - 1, day);
  } else {
    form.endDate = null;
  }
  
  if (props.project.users && props.project.users.length > 0) {
    form.projectManagers = props.project.users.map(u => u.id);
    mergeManagerOptions(props.project.users);
  } else {
    form.projectManagers = [];
  }
};

const formatDate = (value) => {
  if (!value) return null;
  const year = value.getFullYear();
  const month = `${value.getMonth() + 1}`.padStart(2, '0');
  const day = `${value.getDate()}`.padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const mergeManagerOptions = (users = []) => {
  const map = new Map(managerOptions.value.map((user) => [user.id, user]));
  users.forEach((user) => {
    if (user?.id) {
      map.set(user.id, user);
    }
  });
  managerOptions.value = Array.from(map.values()).sort((a, b) =>
    a.name.localeCompare(b.name, 'ru'),
  );
};

const fetchManagers = async (search = '') => {
  isManagersLoading.value = true;
  try {
    const items = await store.dispatch('users/searchProjectManagers', {
      search,
      limit: 10,
    });
    mergeManagerOptions(items ?? []);
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось загрузить менеджеров',
      detail: error.message ?? 'Попробуйте ещё раз.',
      life: 5000,
    });
  } finally {
    isManagersLoading.value = false;
  }
};

const handleManagersFilter = (event) => {
  const term = event.value ?? '';
  if (managerFilterTimer) {
    clearTimeout(managerFilterTimer);
  }
  managerFilterTimer = setTimeout(() => fetchManagers(term), 350);
};

onBeforeUnmount(() => {
  if (managerFilterTimer) {
    clearTimeout(managerFilterTimer);
  }
});

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      loadProjectData();
      fetchManagers();
    } else {
      resetForm();
    }
  },
);

watch(
  () => props.project,
  () => {
    if (props.visible && props.project) {
      loadProjectData();
    }
  },
);

const isSubmitDisabled = computed(() => !form.name.trim() || isSubmitting.value);

const buildPayload = () => ({
  name: form.name.trim(),
  isActive: form.isActive,
  clientName: form.clientName.trim() || null,
  startDate: formatDate(form.startDate),
  endDate: formatDate(form.endDate),
  deadlineType: form.deadlineType,
  users: form.projectManagers,
});

const handleSubmit = async () => {
  if (!form.name.trim()) {
    toast.add({
      severity: 'warn',
      summary: 'Название обязательно',
      detail: 'Введите название проекта.',
      life: 4000,
    });
    return;
  }

  if (dateError.value) {
    toast.add({
      severity: 'warn',
      summary: 'Проверьте даты',
      detail: dateError.value,
      life: 4000,
    });
    return;
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
    const payload = buildPayload();
    const updated = await store.dispatch('projects/update', {
      id: props.project.id,
      payload,
    });

    toast.add({
      severity: 'success',
      summary: 'Проект обновлён',
      detail: `«${updated.name}» успешно обновлён.`,
      life: 3000,
    });
    internalVisible.value = false;
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка обновления проекта',
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
    header="Редактировать проект"
    modal
    dismissableMask
    :draggable="false"
    blockScroll
    class="edit-project-dialog"
    :style="{ width: '720px', maxWidth: '95vw' }"
  >
    <form class="d-flex flex-column gap-4" @submit.prevent="handleSubmit">
      <div class="row g-3">
        <div class="col-md-8">
          <label class="form-label fw-semibold">Название проекта</label>
          <InputText
            v-model="form.name"
            class="w-100"
            placeholder="Например, Fortes Vision"
            autocomplete="off"
          />
        </div>
        <div class="col-md-4">
          <label class="form-label fw-semibold d-block">Статус</label>
          <div class="d-flex align-items-center gap-2">
            <ToggleSwitch inputId="edit-project-active" v-model="form.isActive" />
            <label class="mb-0 text-muted" for="edit-project-active">
              {{ form.isActive ? 'Активен' : 'Приостановлен' }}
            </label>
          </div>
        </div>
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label fw-semibold">Дата начала</label>
          <DatePicker
            v-model="form.startDate"
            showIcon
            iconDisplay="input"
            dateFormat="dd.mm.yy"
            class="w-100"
            placeholder="Не выбрано"
          />
        </div>
        <div class="col-md-6">
          <label class="form-label fw-semibold">Дата окончания</label>
          <DatePicker
            v-model="form.endDate"
            showIcon
            iconDisplay="input"
            dateFormat="dd.mm.yy"
            class="w-100"
            placeholder="Не выбрано"
          />
          <small v-if="dateError" class="text-danger">{{ dateError }}</small>
        </div>
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label fw-semibold">Клиент</label>
          <InputText
            v-model="form.clientName"
            class="w-100"
            placeholder="Имя клиента"
            autocomplete="off"
          />
        </div>
        <div class="col-md-6">
          <label class="form-label fw-semibold">Тип дедлайна</label>
          <Select
            v-model="form.deadlineType"
            :options="deadlineOptions"
            optionLabel="label"
            optionValue="value"
            class="w-100"
          />
        </div>
      </div>

      <div>
        <label class="form-label fw-semibold d-block">Project managers</label>
        <MultiSelect
          v-model="form.projectManagers"
          :options="managerOptions"
          optionLabel="name"
          optionValue="id"
          display="chip"
          filter
          :loading="isManagersLoading"
          class="w-100"
          placeholder="Выберите менеджеров"
          filterPlaceholder="Поиск по имени"
          :maxSelectedLabels="3"
          @filter="handleManagersFilter"
        />
        <small class="text-muted">

        </small>
      </div>

      <div class="d-flex justify-content-end gap-2">
        <Button type="button" label="Отмена" severity="secondary" class="px-4" @click="internalVisible = false" />
        <Button
          type="submit"
          label="Сохранить"
          class="px-4"
          :disabled="isSubmitDisabled || Boolean(dateError)"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.edit-project-dialog :deep(.p-dialog-content) {
  padding-top: 0.75rem;
}

.form-label {
  color: #475569;
}

form :deep(.p-inputtext),
form :deep(.p-multiselect),
form :deep(.p-select),
form :deep(.p-datepicker) {
  width: 100%;
}
</style>




