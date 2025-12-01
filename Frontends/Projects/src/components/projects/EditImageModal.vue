<script setup>
import { computed, reactive, ref, watch, onBeforeUnmount } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import { updateImage, getImages } from '../../repositories/imagesRepository';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
  image: {
    type: Object,
    default: null,
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

const emit = defineEmits(['update:visible', 'image-updated']);

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
const isLoading = ref(false);
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

const loadImageData = async () => {
  if (!props.image || !props.batch || !props.project) {
    resetForm();
    return;
  }

  form.name = props.image.name || '';
  isLoading.value = true;
  
  try {
    // Загружаем полные данные изображения с пользователями
    const images = await getImages(props.project.id, props.batch.id);
    const fullImage = images.find(img => img.id === props.image.id);
    
    if (fullImage && fullImage.users) {
      // Группируем пользователей по ролям
      form.modellers = fullImage.users
        .filter(u => u.role === 'modeller')
        .map(u => u.id);
      
      form.freelancers = fullImage.users
        .filter(u => u.role === 'freelancer')
        .map(u => u.id);
      
      form.artDirectors = fullImage.users
        .filter(u => u.role === 'art_director')
        .map(u => u.id);
      
      form.artists = fullImage.users
        .filter(u => u.role === 'artist')
        .map(u => u.id);
      
      // Добавляем пользователей в опции
      mergeUserOptions(fullImage.users);
    }
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка загрузки',
      detail: error.message ?? 'Не удалось загрузить данные изображения.',
      life: 5000,
    });
  } finally {
    isLoading.value = false;
  }
};

const mergeUserOptions = (users = []) => {
  const map = new Map(userOptions.value.map((user) => [user.id, user]));
  users.forEach((user) => {
    if (user?.id) {
      map.set(user.id, {
        id: user.id,
        name: user.name,
        role: user.role ?? user.Role ?? '',
      });
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
  async (visible) => {
    if (visible) {
      await loadImageData();
      fetchUsers();
    } else {
      resetForm();
    }
  },
);

watch(
  () => props.image,
  async () => {
    if (props.visible && props.image) {
      await loadImageData();
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

  if (!props.image?.id || !props.batch?.id || !props.project?.id) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка',
      detail: 'Изображение, батч или проект не выбран.',
      life: 5000,
    });
    return;
  }

  isSubmitting.value = true;
  try {
    const users = buildUsersPayload();
    await updateImage(props.project.id, props.batch.id, props.image.id, {
      name: form.name.trim(),
      users: users,
    });

    toast.add({
      severity: 'success',
      summary: 'Изображение обновлено',
      detail: `«${form.name}» успешно обновлено.`,
      life: 3000,
    });
    
    emit('image-updated', props.project.id, props.batch.id);
    internalVisible.value = false;
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Ошибка обновления изображения',
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
    header="Редактировать изображение"
    modal
    dismissableMask
    :draggable="false"
    blockScroll
    class="edit-image-dialog"
    :style="{ width: '720px', maxWidth: '95vw' }"
  >
    <div v-if="isLoading" class="text-center py-5">
      <i class="pi pi-spin pi-spinner fs-4 d-block mb-2"></i>
      <span>Загрузка данных...</span>
    </div>

    <form v-else class="d-flex flex-column gap-4" @submit.prevent="handleSubmit">
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
          label="Сохранить"
          class="px-4"
          :disabled="!isFormValid || isSubmitting"
          :loading="isSubmitting"
        />
      </div>
    </form>
  </Dialog>
</template>

<style scoped>
.edit-image-dialog :deep(.p-dialog-content) {
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


