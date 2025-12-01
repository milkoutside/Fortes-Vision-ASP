<script setup>
import { computed, reactive, ref, watch, onBeforeUnmount } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import { createImage } from '../../repositories/imagesRepository';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
  batch: {
    type: Object,
    default: null,
  },
  project: {
    type: Object,
    default: null,
  },
});

const emit = defineEmits(['update:visible', 'image-created']);

const internalVisible = computed({
  get: () => props.visible,
  set: (value) => emit('update:visible', value),
});

const store = useStore();
const toast = useToast();

const form = reactive({
  name: '',
  modellers: [],
  freelancers: [],
  artDirectors: [],
  artists: [],
});

const isSubmitting = ref(false);
const userOptions = ref([]);
const isLoadingUsers = ref(false);
let userFilterTimer;

const getRoleKey = (user) => (user?.role ?? user?.Role ?? '').toLowerCase();
const createRoleOptions = (role) =>
  computed(() => userOptions.value.filter((user) => getRoleKey(user) === role));

const modellerOptions = createRoleOptions('modeller');
const freelancerOptions = createRoleOptions('freelancer');
const artDirectorOptions = createRoleOptions('art_director');
const artistOptions = createRoleOptions('artist');

const resetForm = () => {
  form.name = '';
  form.modellers = [];
  form.freelancers = [];
  form.artDirectors = [];
  form.artists = [];
};

const mergeUserOptions = (users = []) => {
  const map = new Map(userOptions.value.map((user) => [user.id, user]));
  users.forEach((user) => {
    if (user?.id) {
      map.set(user.id, user);
    }
  });
  userOptions.value = Array.from(map.values()).sort((a, b) =>
    a.name.localeCompare(b.name, 'ru'),
  );
};

const fetchUsers = async (search = '') => {
  isLoadingUsers.value = true;
  try {
    const items = await store.dispatch('users/search', {
      search,
      limit: 20,
    });
    mergeUserOptions(items ?? []);
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Не удалось загрузить пользователей',
      detail: error.message ?? 'Попробуйте ещё раз.',
      life: 5000,
    });
  } finally {
    isLoadingUsers.value = false;
  }
};

const handleUsersFilter = (event) => {
  const term = event.value ?? '';
  if (userFilterTimer) {
    clearTimeout(userFilterTimer);
  }
  userFilterTimer = setTimeout(() => fetchUsers(term), 350);
};

onBeforeUnmount(() => {
  if (userFilterTimer) {
    clearTimeout(userFilterTimer);
  }
});

watch(
  () => props.visible,
  (visible) => {
    if (visible) {
      resetForm();
      fetchUsers();
    }
  },
);

const isFormValid = computed(() => {
  return form.name.trim().length > 0;
});

const buildUsersPayload = () => {
  const users = [];
  
  form.modellers.forEach(userId => {
    users.push({ userId, role: 'modeller' });
  });
  
  form.freelancers.forEach(userId => {
    users.push({ userId, role: 'freelancer' });
  });
  
  form.artDirectors.forEach(userId => {
    users.push({ userId, role: 'art_director' });
  });
  
  form.artists.forEach(userId => {
    users.push({ userId, role: 'artist' });
  });
  
  return users;
};

const handleSubmit = async () => {
  if (!form.name.trim()) {
    toast.add({
      severity: 'warn',
      summary: 'Название обязательно',
      detail: 'Введите название изображения.',
      life: 4000,
    });
    return;
  }

  if (!props.batch?.id || !props.project?.id) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: 'Батч или проект не выбран.',
      life: 5000,
    });
    return;
  }

  isSubmitting.value = true;
  try {
    const users = buildUsersPayload();
    await createImage(props.project.id, props.batch.id, {
      name: form.name.trim(),
      users: users.length > 0 ? users : null,
    });

    toast.add({
      severity: 'success',
      summary: 'Изображение создано',
      detail: `«${form.name}» успешно создано.`,
      life: 3000,
    });
    
    emit('image-created', props.project.id, props.batch.id);
    internalVisible.value = false;
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка создания изображения',
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
    header="Создать изображение"
    modal
    dismissableMask
    :draggable="false"
    blockScroll
    class="create-image-dialog"
    :style="{ width: '720px', maxWidth: '95vw' }"
  >
    <form class="d-flex flex-column gap-4" @submit.prevent="handleSubmit">
      <div>
        <label class="form-label fw-semibold">Название изображения</label>
        <InputText
          v-model="form.name"
          class="w-100"
          placeholder="Введите название изображения"
          autocomplete="off"
        />
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label fw-semibold">Modellers</label>
          <MultiSelect
            v-model="form.modellers"
            :options="modellerOptions"
            optionLabel="name"
            optionValue="id"
            display="chip"
            filter
            :loading="isLoadingUsers"
            class="w-100"
            placeholder="Выберите modellers"
            filterPlaceholder="Поиск по имени"
            :maxSelectedLabels="3"
            @filter="handleUsersFilter"
          />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-semibold">Freelancers</label>
          <MultiSelect
            v-model="form.freelancers"
            :options="freelancerOptions"
            optionLabel="name"
            optionValue="id"
            display="chip"
            filter
            :loading="isLoadingUsers"
            class="w-100"
            placeholder="Выберите freelancers"
            filterPlaceholder="Поиск по имени"
            :maxSelectedLabels="3"
            @filter="handleUsersFilter"
          />
        </div>
      </div>

      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label fw-semibold">Art Directors</label>
          <MultiSelect
            v-model="form.artDirectors"
            :options="artDirectorOptions"
            optionLabel="name"
            optionValue="id"
            display="chip"
            filter
            :loading="isLoadingUsers"
            class="w-100"
            placeholder="Выберите art directors"
            filterPlaceholder="Поиск по имени"
            :maxSelectedLabels="3"
            @filter="handleUsersFilter"
          />
        </div>

        <div class="col-md-6">
          <label class="form-label fw-semibold">Artists</label>
          <MultiSelect
            v-model="form.artists"
            :options="artistOptions"
            optionLabel="name"
            optionValue="id"
            display="chip"
            filter
            :loading="isLoadingUsers"
            class="w-100"
            placeholder="Выберите artists"
            filterPlaceholder="Поиск по имени"
            :maxSelectedLabels="3"
            @filter="handleUsersFilter"
          />
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
.create-image-dialog :deep(.p-dialog-content) {
  padding-top: 0.75rem;
}

.form-label {
  color: #475569;
}

form :deep(.p-inputtext),
form :deep(.p-multiselect) {
  width: 100%;
}
</style>


