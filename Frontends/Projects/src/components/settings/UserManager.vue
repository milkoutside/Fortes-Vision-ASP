<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';

const RAW_ROLES = ['modeller', 'freelancer', 'artist', 'art_director', 'project_manager'];
const humanizeRole = (value) =>
  value
    .split('_')
    .map((chunk) => chunk.charAt(0).toUpperCase() + chunk.slice(1))
    .join(' ');
const ROLE_OPTIONS = RAW_ROLES.map((value) => ({ value, label: humanizeRole(value) }));
const roleFilterOptions = [{ label: 'Все роли', value: 'all' }, ...ROLE_OPTIONS];

const store = useStore();
const toast = useToast();
const confirm = useConfirm();

const searchTerm = ref('');
const roleFilter = ref('all');
const searchDebounce = ref();
const selectedPageSize = ref(10);
const pageSizeOptions = [5, 10, 20, 50];

const createForm = reactive({
  name: '',
  role: ROLE_OPTIONS[0].value,
});

const editingRow = reactive({
  id: null,
  name: '',
  role: '',
});

const users = computed(() => store.state.users.items);
const isLoading = computed(() => store.state.users.isLoading);
const isSaving = computed(() => store.state.users.isSaving);
const pagination = computed(() => store.state.users.pagination);
const hasLoaded = computed(() => store.state.users.hasLoaded);

const loadUsers = async ({ page, limit } = {}) => {
  const targetPage = page ?? pagination.value.currentPage ?? 1;
  const targetLimit = limit ?? selectedPageSize.value;
  selectedPageSize.value = targetLimit;

  try {
    await store.dispatch('users/fetchAll', {
      search: searchTerm.value.trim() || undefined,
      role: roleFilter.value,
      page: targetPage,
      limit: targetLimit,
    });
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка загрузки пользователей',
      detail: error.message ?? 'Не удалось получить пользователей.',
      life: 5000,
    });
  }
};

const ensureData = async () => {
  if (!hasLoaded.value) {
    await loadUsers({ page: 1, limit: selectedPageSize.value });
  } else {
    selectedPageSize.value = pagination.value.perPage;
  }
};

onMounted(ensureData);

watch(
  () => searchTerm.value,
  () => {
    if (searchDebounce.value) clearTimeout(searchDebounce.value);
    searchDebounce.value = setTimeout(() => {
      loadUsers({ page: 1, limit: selectedPageSize.value });
    }, 400);
  },
);

watch(
  () => roleFilter.value,
  () => {
    loadUsers({ page: 1, limit: selectedPageSize.value });
  },
);

const isCreateModalVisible = ref(false);

const resetCreateForm = () => {
  createForm.name = '';
  createForm.role = ROLE_OPTIONS[0].value;
};

const resetEditingRow = () => {
  editingRow.id = null;
  editingRow.name = '';
  editingRow.role = '';
};

const openCreateModal = () => {
  resetCreateForm();
  isCreateModalVisible.value = true;
};

const handleCreateDialogHide = () => {
  resetCreateForm();
};

const handleCreateSubmit = async () => {
  if (!createForm.name.trim()) {
    toast.add({
      severity: 'warn',
      summary: 'Проверьте данные',
      detail: 'Имя обязательно.',
      life: 4000,
    });
    return;
  }

  const payload = {
    name: createForm.name.trim(),
    role: createForm.role,
  };

  try {
    await store.dispatch('users/create', payload);
    toast.add({
      severity: 'success',
      summary: 'Пользователь создан',
      detail: `«${payload.name}» добавлен.`,
      life: 3000,
    });
    isCreateModalVisible.value = false;
    await loadUsers({ page: 1, limit: selectedPageSize.value });
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось создать',
      detail: error.message ?? 'Попробуйте ещё раз.',
      life: 5000,
    });
  }
};

const startEditing = (user) => {
  editingRow.id = user.id;
  editingRow.name = user.name;
  editingRow.role = user.role;
};

const canInlineSave = computed(() => !!editingRow.id && !!editingRow.name.trim());

const saveEditing = async () => {
  if (!canInlineSave.value) {
    toast.add({
      severity: 'warn',
      summary: 'Проверьте данные',
      detail: 'Имя и роль обязательны.',
      life: 4000,
    });
    return;
  }

  const payload = {
    name: editingRow.name.trim(),
    role: editingRow.role,
  };

  try {
    await store.dispatch('users/update', { id: editingRow.id, payload });
    toast.add({
      severity: 'success',
      summary: 'Пользователь обновлён',
      detail: `«${payload.name}» сохранён.`,
      life: 3000,
    });
    resetEditingRow();
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось сохранить',
      detail: error.message ?? 'Попробуйте ещё раз.',
      life: 5000,
    });
  }
};

const cancelEditing = () => {
  resetEditingRow();
};

const getRoleLabel = (value) => ROLE_OPTIONS.find((role) => role.value === value)?.label ?? value;

const confirmRemoval = (user) => {
  confirm.require({
    message: `Удалить пользователя «${user.name}»?`,
    header: 'Удаление пользователя',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Удалить',
    rejectLabel: 'Отмена',
    acceptClass: 'p-button-danger',
    accept: async () => {
      try {
        await store.dispatch('users/delete', user.id);
        toast.add({
          severity: 'success',
          summary: 'Пользователь удалён',
          detail: `«${user.name}» больше не существует.`,
          life: 3000,
        });
        if (editingRow.id === user.id) {
          resetEditingRow();
        }
        const nextPage = Math.min(pagination.value.lastPage, pagination.value.currentPage);
        await loadUsers({ page: nextPage, limit: selectedPageSize.value });
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка удаления',
          detail: error.message ?? 'Не удалось удалить пользователя.',
          life: 5000,
        });
      }
    },
  });
};

const handlePageChange = (event) => {
  const nextPage = event.page + 1;
  const nextRows = event.rows;
  loadUsers({ page: nextPage, limit: nextRows });
};
</script>

<template>
  <section class="user-manager container-fluid py-3">
    <div class="card shadow-sm border-0 rounded-4">
      <div class="card-body d-flex flex-column">
        <div class="d-flex flex-column flex-lg-row justify-content-between align-items-lg-center gap-3 mb-3">
          <div>
            <h5 class="mb-1">Список пользователей</h5>
            <small class="text-muted">Всего: {{ pagination.total }}</small>
          </div>
          <div class="control-toolbar d-flex flex-column flex-lg-row align-items-stretch gap-2 w-100 w-lg-auto">
            <div class="search-box d-flex align-items-center gap-2 flex-grow-1">
              <span class="pi pi-search text-muted"></span>
              <InputText
                v-model="searchTerm"
                placeholder="Поиск по имени"
                class="flex-grow-1 border-0 shadow-none"
              />
            </div>
            <Select
              v-model="roleFilter"
              :options="roleFilterOptions"
              optionLabel="label"
              optionValue="value"
              class="role-filter"
              :disabled="isLoading"
            />
            <Button
              type="button"
              label="Создать"
              icon="pi pi-user-plus"
              class="create-btn"
              size="small"
              @click="openCreateModal"
            />
          </div>
        </div>

        <DataTable
          :value="users"
          :loading="isLoading"
          dataKey="id"
          scrollable
          scrollHeight="420px"
          class="flex-grow-1"
        >
          <Column field="name" header="Имя">
            <template #body="{ data }">
              <div v-if="editingRow.id === data.id" class="inline-input">
                <InputText
                  v-model.trim="editingRow.name"
                  placeholder="Введите имя"
                  :disabled="isSaving"
                />
              </div>
              <span v-else>{{ data.name }}</span>
            </template>
          </Column>

          <Column field="role" header="Роль">
            <template #body="{ data }">
              <div v-if="editingRow.id === data.id" class="inline-input">
                <Select
                  v-model="editingRow.role"
                  :options="ROLE_OPTIONS"
                  optionLabel="label"
                  optionValue="value"
                  placeholder="Выберите роль"
                  class="w-100"
                  :disabled="isSaving"
                />
              </div>
              <span v-else class="badge bg-soft-primary px-3 py-2">
                {{ getRoleLabel(data.role) }}
              </span>
            </template>
          </Column>

          <Column header="Действия" class="text-end">
            <template #body="{ data }">
              <div v-if="editingRow.id === data.id" class="d-flex gap-2 justify-content-end">
                <Button
                  icon="pi pi-check"
                  severity="success"
                  rounded
                  size="small"
                  :disabled="!canInlineSave || isSaving"
                  :loading="isSaving"
                  @click="saveEditing"
                />
                <Button
                  icon="pi pi-times"
                  severity="secondary"
                  rounded
                  size="small"
                  outlined
                  :disabled="isSaving"
                  @click="cancelEditing"
                />
              </div>
              <div v-else class="d-flex gap-2 justify-content-end">
                <Button
                  icon="pi pi-pencil"
                  severity="secondary"
                  rounded
                  size="small"
                  outlined
                  @click="startEditing(data)"
                />
                <Button
                  icon="pi pi-trash"
                  severity="danger"
                  rounded
                  size="small"
                  outlined
                  @click="confirmRemoval(data)"
                />
              </div>
            </template>
          </Column>

          <template #empty>
            <div class="text-center text-muted py-4">Пользователи не найдены.</div>
          </template>
        </DataTable>

        <Paginator
          class="mt-3"
          :rows="pagination.perPage"
          :totalRecords="pagination.total"
          :rowsPerPageOptions="pageSizeOptions"
          :first="(pagination.currentPage - 1) * pagination.perPage"
          @page="handlePageChange"
        />
      </div>
    </div>

    <Dialog
      v-model:visible="isCreateModalVisible"
      modal
      header="Создать пользователя"
      :style="{ width: '420px' }"
      :draggable="false"
      @hide="handleCreateDialogHide"
    >
      <form class="d-flex flex-column gap-3" @submit.prevent="handleCreateSubmit">
        <div>
          <label class="form-label fw-semibold">Имя</label>
          <InputText
            v-model.trim="createForm.name"
            placeholder="Полное имя"
            class="w-100"
            :disabled="isSaving"
          />
        </div>

        <div>
          <label class="form-label fw-semibold">Роль</label>
          <Select
            v-model="createForm.role"
            :options="ROLE_OPTIONS"
            optionLabel="label"
            optionValue="value"
            class="w-100"
            :disabled="isSaving"
          />
        </div>
      </form>

      <template #footer>
        <div class="d-flex justify-content-end gap-2">
          <Button
            label="Отмена"
            severity="secondary"
            outlined
            @click="isCreateModalVisible = false"
          />
          <Button
            label="Создать"
            icon="pi pi-check"
            :loading="isSaving"
            :disabled="!createForm.name.trim() || isSaving"
            @click="handleCreateSubmit"
          />
        </div>
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.badge.bg-soft-primary {
  background-color: rgba(59, 130, 246, 0.12);
  color: #2563eb;
  text-transform: capitalize;
}

.control-toolbar > * {
  min-height: 36px;
}

.search-box {
  max-width: 320px;
  border-radius: 999px;
  border: 1px solid rgba(15, 23, 42, 0.12);
  padding: 0.2rem 0.75rem;
  background-color: transparent;
}

.search-box :deep(.p-inputtext) {
  padding: 0;
  min-height: 32px;
}

.inline-input :deep(.p-inputtext),
.inline-input :deep(.p-select) {
  width: 100%;
}

.role-filter {
  min-width: 200px;
}

.role-filter :deep(.p-select) {
  width: 100%;
  min-height: 36px;
}

.create-btn {
  min-width: 160px;
  min-height: 36px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
</style>

