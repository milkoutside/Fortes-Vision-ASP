<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useStore } from 'vuex';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import ProjectContextMenu from './ProjectContextMenu.vue';
import EditProjectModal from './EditProjectModal.vue';
import CreateBatchModal from './CreateBatchModal.vue';
import BatchContextMenu from './BatchContextMenu.vue';
import EditBatchModal from './EditBatchModal.vue';
import ImageContextMenu from './ImageContextMenu.vue';
import CreateImageModal from './CreateImageModal.vue';
import EditImageModal from './EditImageModal.vue';
import { getBatches } from '../../repositories/batchesRepository';
import { getImages, deleteImage } from '../../repositories/imagesRepository';

const store = useStore();
const toast = useToast();
const confirm = useConfirm();

const searchInput = ref('');
const searchTimeout = ref(null);
const listContainer = ref(null);

const projects = computed(() => store.state.projects.items);
const isLoading = computed(() => store.state.projects.isLoading);
const isLoadingMore = computed(() => store.state.projects.isLoadingMore);
const hasMore = computed(() => store.state.projects.hasMore);
const error = computed(() => store.state.projects.error);

const contextMenuVisible = ref(false);
const contextMenuX = ref(0);
const contextMenuY = ref(0);
const selectedProject = ref(null);
const isEditModalOpen = ref(false);
const isCreateBatchModalOpen = ref(false);

const expandedProjects = ref(new Set());
const batchesByProject = ref(new Map());
const loadingBatches = ref(new Set());

const expandedBatches = ref(new Set());
const imagesByBatch = ref(new Map());
const loadingImages = ref(new Set());

const handleSearch = () => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value);
  }
  
  searchTimeout.value = setTimeout(() => {
    store.dispatch('projects/search', searchInput.value || '');
  }, 300);
};

const handleScroll = () => {
  if (!listContainer.value || isLoadingMore.value || !hasMore.value) return;
  
  const container = listContainer.value;
  const scrollTop = container.scrollTop;
  const scrollHeight = container.scrollHeight;
  const clientHeight = container.clientHeight;
  
  // Загружаем следующую страницу, когда пользователь прокрутил до 80% списка
  if (scrollTop + clientHeight >= scrollHeight * 0.8) {
    store.dispatch('projects/loadMore');
  }
};

const handleProjectClick = async (project, event) => {
  // Предотвращаем открытие при клике на контекстное меню
  if (event?.target?.closest('.batch-item') || event?.target?.closest('.project-context-menu')) {
    return;
  }
  
  const projectId = project.id;
  
  if (expandedProjects.value.has(projectId)) {
    // Закрываем проект
    expandedProjects.value.delete(projectId);
  } else {
    // Открываем проект и загружаем батчи
    expandedProjects.value.add(projectId);
    
    if (!batchesByProject.value.has(projectId)) {
      loadingBatches.value.add(projectId);
      try {
        const batches = await getBatches(projectId);
        batchesByProject.value.set(projectId, batches);
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка загрузки батчей',
          detail: error.message ?? 'Не удалось загрузить батчи проекта.',
          life: 5000,
        });
      } finally {
        loadingBatches.value.delete(projectId);
      }
    }
  }
};

const handleContextMenu = (event, project) => {
  event.preventDefault();
  event.stopPropagation();
  // Закрываем другие меню
  batchContextMenuVisible.value = false;
  imageContextMenuVisible.value = false;
  // Закрываем меню календаря через событие
  window.dispatchEvent(new CustomEvent('close-calendar-context-menu'));
  
  selectedProject.value = project;
  contextMenuX.value = event.clientX;
  contextMenuY.value = event.clientY;
  contextMenuVisible.value = true;
};

const handleEdit = () => {
  if (selectedProject.value) {
    isEditModalOpen.value = true;
  }
};

const handleCreateBatch = () => {
  if (selectedProject.value) {
    isCreateBatchModalOpen.value = true;
  }
};

const handleDelete = () => {
  if (!selectedProject.value) return;
  
  const projectName = selectedProject.value.name;
  confirm.require({
    message: `Вы уверены, что хотите удалить проект «${projectName}»? Это действие нельзя отменить.`,
    header: 'Подтверждение удаления',
    icon: 'pi pi-exclamation-triangle',
    rejectClass: 'p-button-secondary p-button-outlined',
    rejectLabel: 'Отмена',
    acceptLabel: 'Удалить',
    accept: async () => {
      try {
        await store.dispatch('projects/delete', selectedProject.value.id);
        expandedProjects.value.delete(selectedProject.value.id);
        batchesByProject.value.delete(selectedProject.value.id);
        toast.add({
          severity: 'success',
          summary: 'Проект удалён',
          detail: `«${projectName}» успешно удалён.`,
          life: 3000,
        });
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка удаления',
          detail: error.message ?? 'Не удалось удалить проект.',
          life: 6000,
        });
      }
    },
  });
};

const batchContextMenuVisible = ref(false);
const batchContextMenuX = ref(0);
const batchContextMenuY = ref(0);
const selectedBatch = ref(null);
const selectedBatchProject = ref(null);
const isEditBatchModalOpen = ref(false);
const isCreateImageModalOpen = ref(false);

const imageContextMenuVisible = ref(false);
const imageContextMenuX = ref(0);
const imageContextMenuY = ref(0);
const selectedImage = ref(null);
const selectedImageBatch = ref(null);
const selectedImageProject = ref(null);
const isEditImageModalOpen = ref(false);

const handleBatchContextMenu = (event, batch, project) => {
  event.preventDefault();
  event.stopPropagation();
  // Закрываем другие меню
  contextMenuVisible.value = false;
  imageContextMenuVisible.value = false;
  // Закрываем меню календаря через событие
  window.dispatchEvent(new CustomEvent('close-calendar-context-menu'));
  
  selectedBatch.value = batch;
  selectedBatchProject.value = project;
  batchContextMenuX.value = event.clientX;
  batchContextMenuY.value = event.clientY;
  batchContextMenuVisible.value = true;
};

const handleBatchEdit = () => {
  if (selectedBatch.value) {
    isEditBatchModalOpen.value = true;
  }
};

const handleBatchDelete = () => {
  if (!selectedBatch.value || !selectedBatchProject.value) return;
  
  const batchName = selectedBatch.value.name;
  confirm.require({
    message: `Вы уверены, что хотите удалить батч «${batchName}»? Это действие нельзя отменить.`,
    header: 'Подтверждение удаления',
    icon: 'pi pi-exclamation-triangle',
    rejectClass: 'p-button-secondary p-button-outlined',
    rejectLabel: 'Отмена',
    acceptLabel: 'Удалить',
    accept: async () => {
      try {
        const { deleteBatch } = await import('../../repositories/batchesRepository');
        await deleteBatch(selectedBatchProject.value.id, selectedBatch.value.id);
        
        // Обновляем список батчей
        const batches = batchesByProject.value.get(selectedBatchProject.value.id) || [];
        const updatedBatches = batches.filter(b => b.id !== selectedBatch.value.id);
        batchesByProject.value.set(selectedBatchProject.value.id, updatedBatches);
        
        toast.add({
          severity: 'success',
          summary: 'Батч удалён',
          detail: `«${batchName}» успешно удалён.`,
          life: 3000,
        });
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка удаления',
          detail: error.message ?? 'Не удалось удалить батч.',
          life: 6000,
        });
      }
    },
  });
};

const handleBatchClick = async (batch, project, event) => {
  // Предотвращаем открытие при клике на контекстное меню или изображение
  if (event?.target?.closest('.image-item') || event?.target?.closest('.batch-context-menu') || event?.target?.closest('.image-context-menu')) {
    return;
  }
  
  const batchKey = `${project.id}-${batch.id}`;
  
  if (expandedBatches.value.has(batchKey)) {
    // Закрываем батч
    expandedBatches.value.delete(batchKey);
  } else {
    // Открываем батч и загружаем изображения
    expandedBatches.value.add(batchKey);
    
    if (!imagesByBatch.value.has(batchKey)) {
      loadingImages.value.add(batchKey);
      try {
        const images = await getImages(project.id, batch.id);
        imagesByBatch.value.set(batchKey, images);
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка загрузки изображений',
          detail: error.message ?? 'Не удалось загрузить изображения батча.',
          life: 5000,
        });
      } finally {
        loadingImages.value.delete(batchKey);
      }
    }
  }
};

const handleBatchCreateImages = () => {
  if (selectedBatch.value && selectedBatchProject.value) {
    isCreateImageModalOpen.value = true;
  }
};

const handleBatchCreated = async (projectId) => {
  // Обновляем список батчей после создания
  if (expandedProjects.value.has(projectId)) {
    loadingBatches.value.add(projectId);
    try {
      const batches = await getBatches(projectId);
      batchesByProject.value.set(projectId, batches);
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Ошибка обновления',
        detail: 'Не удалось обновить список батчей.',
        life: 3000,
      });
    } finally {
      loadingBatches.value.delete(projectId);
    }
  }
};

const handleBatchUpdated = async (projectId) => {
  // Обновляем список батчей после редактирования
  if (expandedProjects.value.has(projectId)) {
    loadingBatches.value.add(projectId);
    try {
      const batches = await getBatches(projectId);
      batchesByProject.value.set(projectId, batches);
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Ошибка обновления',
        detail: 'Не удалось обновить список батчей.',
        life: 3000,
      });
    } finally {
      loadingBatches.value.delete(projectId);
    }
  }
};

const handleImageContextMenu = (event, image, batch, project) => {
  event.preventDefault();
  event.stopPropagation();
  // Закрываем другие меню
  contextMenuVisible.value = false;
  batchContextMenuVisible.value = false;
  // Закрываем меню календаря через событие
  window.dispatchEvent(new CustomEvent('close-calendar-context-menu'));
  
  selectedImage.value = image;
  selectedImageBatch.value = batch;
  selectedImageProject.value = project;
  imageContextMenuX.value = event.clientX;
  imageContextMenuY.value = event.clientY;
  imageContextMenuVisible.value = true;
};

const handleImageEdit = () => {
  if (selectedImage.value) {
    isEditImageModalOpen.value = true;
  }
};

const handleImageDelete = () => {
  if (!selectedImage.value || !selectedImageBatch.value || !selectedImageProject.value) return;
  
  const imageName = selectedImage.value.name;
  confirm.require({
    message: `Вы уверены, что хотите удалить изображение «${imageName}»? Это действие нельзя отменить.`,
    header: 'Подтверждение удаления',
    icon: 'pi pi-exclamation-triangle',
    rejectClass: 'p-button-secondary p-button-outlined',
    rejectLabel: 'Отмена',
    acceptLabel: 'Удалить',
    accept: async () => {
      try {
        await deleteImage(selectedImageProject.value.id, selectedImageBatch.value.id, selectedImage.value.id);
        
        // Обновляем список изображений
        const batchKey = `${selectedImageProject.value.id}-${selectedImageBatch.value.id}`;
        const images = imagesByBatch.value.get(batchKey) || [];
        const updatedImages = images.filter(img => img.id !== selectedImage.value.id);
        imagesByBatch.value.set(batchKey, updatedImages);
        
        toast.add({
          severity: 'success',
          summary: 'Изображение удалено',
          detail: `«${imageName}» успешно удалено.`,
          life: 3000,
        });
      } catch (error) {
        toast.add({
          severity: 'error',
          summary: 'Ошибка удаления',
          detail: error.message ?? 'Не удалось удалить изображение.',
          life: 6000,
        });
      }
    },
  });
};

const handleImageCreated = async (projectId, batchId) => {
  // Обновляем список изображений после создания
  const batchKey = `${projectId}-${batchId}`;
  if (expandedBatches.value.has(batchKey)) {
    loadingImages.value.add(batchKey);
    try {
      const images = await getImages(projectId, batchId);
      imagesByBatch.value.set(batchKey, images);
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Ошибка обновления',
        detail: 'Не удалось обновить список изображений.',
        life: 3000,
      });
    } finally {
      loadingImages.value.delete(batchKey);
    }
  }
};

const handleImageUpdated = async (projectId, batchId) => {
  // Обновляем список изображений после редактирования
  const batchKey = `${projectId}-${batchId}`;
  if (expandedBatches.value.has(batchKey)) {
    loadingImages.value.add(batchKey);
    try {
      const images = await getImages(projectId, batchId);
      imagesByBatch.value.set(batchKey, images);
    } catch (error) {
      toast.add({
        severity: 'error',
        summary: 'Ошибка обновления',
        detail: 'Не удалось обновить список изображений.',
        life: 3000,
      });
    } finally {
      loadingImages.value.delete(batchKey);
    }
  }
};

const handleCloseContextMenus = () => {
  contextMenuVisible.value = false;
  batchContextMenuVisible.value = false;
  imageContextMenuVisible.value = false;
};

onMounted(async () => {
  await store.dispatch('projects/fetchAll', { reset: true });
  // Привязываем обработчик скролла после следующего тика, чтобы элемент был точно отрендерен
  setTimeout(() => {
    if (listContainer.value) {
      listContainer.value.addEventListener('scroll', handleScroll);
    }
  }, 0);
  
  // Слушаем событие закрытия меню от календаря
  window.addEventListener('close-projects-context-menus', handleCloseContextMenus);
});

onUnmounted(() => {
  if (searchTimeout.value) {
    clearTimeout(searchTimeout.value);
  }
  if (listContainer.value) {
    listContainer.value.removeEventListener('scroll', handleScroll);
  }
  window.removeEventListener('close-projects-context-menus', handleCloseContextMenus);
});

// Экспортируем состояние для синхронизации с CalendarCells
defineExpose({
  expandedProjects,
  batchesByProject,
  expandedBatches,
  imagesByBatch,
  loadingBatches,
  loadingImages,
  listContainer,
});
</script>

<template>
  <div class="projects-list d-flex flex-column h-100">
    <!-- Поиск -->
    <div class="projects-list__search p-3 border-bottom">
      <div class="position-relative">
        <i class="pi pi-search position-absolute top-50 start-0 translate-middle-y ms-3 text-muted"></i>
        <InputText
          v-model="searchInput"
          placeholder="Поиск проектов..."
          class="w-100 ps-5"
          @update:model-value="handleSearch"
        />
      </div>
    </div>

    <!-- Фильтры -->
    <div class="projects-list__filters p-3 border-bottom">
      <div class="position-relative">
        <i class="pi pi-filter position-absolute top-50 start-0 translate-middle-y ms-3 text-muted"></i>
        <InputText
          placeholder="Фильтры..."
          class="w-100 ps-5"
          disabled
        />
      </div>
    </div>

    <!-- Список проектов -->
    <div
      ref="listContainer"
      class="projects-list__container flex-grow-1 overflow-auto"
    >
      <div v-if="isLoading && projects.length === 0" class="text-center p-3 text-muted">
        <i class="pi pi-spin pi-spinner fs-4 d-block mb-2"></i>
        <span>Загрузка проектов...</span>
      </div>

      <div v-else-if="error && projects.length === 0" class="text-center p-3 text-danger">
        <i class="pi pi-exclamation-triangle fs-4 d-block mb-2"></i>
        <span>{{ error.message || 'Ошибка загрузки проектов' }}</span>
      </div>

      <div v-else-if="projects.length === 0" class="text-center p-3 text-muted">
        <i class="pi pi-folder fs-4 d-block mb-2"></i>
        <span>Проекты не найдены</span>
      </div>

      <div v-else class="projects-list__items">
        <template v-for="project in projects" :key="project.id">
          <div
            class="projects-list__item p-2 border-bottom cursor-pointer"
            :class="{ 'projects-list__item--expanded': expandedProjects.has(project.id) }"
            @click="handleProjectClick(project, $event)"
            @contextmenu="handleContextMenu($event, project)"
          >
            <div class="d-flex align-items-center justify-content-between">
              <div class="flex-grow-1" style="min-width: 0; padding-left: 12px;">
                <div class="fw-semibold text-truncate">{{ project.name }}</div>
                <div v-if="project.clientName" class="text-muted small text-truncate">
                  Клиент: {{ project.clientName }}
                </div>
              </div>
              <div class="d-flex align-items-center" style="flex-shrink: 0; padding-right: 12px;">
                <i 
                  class="pi"
                  :class="expandedProjects.has(project.id) ? 'pi-chevron-down' : 'pi-chevron-right'"
                  style="font-size: 0.75rem;"
                ></i>
              </div>
            </div>
          </div>
          
          <!-- Батчи проекта -->
          <div
            v-if="expandedProjects.has(project.id)"
            class="batches-list"
          >
            <div v-if="loadingBatches.has(project.id)" class="text-center p-2 ps-4 text-muted small batch-item" style="height: 6vh; display: flex; align-items: center; justify-content: center;">
              <i class="pi pi-spin pi-spinner me-2"></i>
              <span>Загрузка батчей...</span>
            </div>
            
            <div
              v-else-if="!batchesByProject.get(project.id) || batchesByProject.get(project.id).length === 0"
              class="text-center p-2 ps-4 text-muted small batch-item"
              style="height: 6vh; display: flex; align-items: center; justify-content: center;"
            >
              Батчи не найдены
            </div>
            
            <template
              v-else
              v-for="batch in batchesByProject.get(project.id)"
              :key="batch.id"
            >
              <div
                class="batch-item p-2 ps-4 border-bottom cursor-pointer"
                :class="{ 'batch-item--expanded': expandedBatches.has(`${project.id}-${batch.id}`) }"
                @click="handleBatchClick(batch, project, $event)"
                @contextmenu="handleBatchContextMenu($event, batch, project)"
              >
                <div class="d-flex align-items-center justify-content-between">
                  <div class="flex-grow-1" style="min-width: 0;">
                    <div class="fw-medium text-truncate">{{ batch.name }}</div>
                  </div>
                  <div class="d-flex align-items-center gap-3 small text-muted" style="flex-shrink: 0;">
                    <span v-if="batch.images?.length" class="text-nowrap">
                      <i class="pi pi-image"></i>
                      {{ batch.images.length }} изображений
                    </span>
                    <span v-if="batch.users?.length" class="text-nowrap">
                      <i class="pi pi-users"></i>
                      {{ batch.users.length }} участников
                    </span>
                    <i 
                      class="pi"
                      :class="expandedBatches.has(`${project.id}-${batch.id}`) ? 'pi-chevron-down' : 'pi-chevron-right'"
                      style="font-size: 0.75rem;"
                    ></i>
                  </div>
                </div>
              </div>
              
              <!-- Изображения батча -->
              <div
                v-if="expandedBatches.has(`${project.id}-${batch.id}`)"
                class="images-list"
              >
                <div v-if="loadingImages.has(`${project.id}-${batch.id}`)" class="text-center p-2 ps-4 text-muted small image-item" style="height: 6vh; display: flex; align-items: center; justify-content: center;">
                  <i class="pi pi-spin pi-spinner me-2"></i>
                  <span>Загрузка изображений...</span>
                </div>
                
                <div
                  v-else-if="!imagesByBatch.get(`${project.id}-${batch.id}`) || imagesByBatch.get(`${project.id}-${batch.id}`).length === 0"
                  class="text-center p-2 ps-4 text-muted small image-item"
                  style="height: 6vh; display: flex; align-items: center; justify-content: center;"
                >
                  Изображения не найдены
                </div>
                
                <div
                  v-else
                  v-for="image in imagesByBatch.get(`${project.id}-${batch.id}`)"
                  :key="image.id"
                  class="image-item p-2 ps-4 border-bottom cursor-pointer"
                  @click.stop
                  @contextmenu="handleImageContextMenu($event, image, batch, project)"
                >
                  <div class="d-flex align-items-center justify-content-between">
                    <div class="fw-medium flex-grow-1 text-truncate" style="min-width: 0;">{{ image.name }}</div>
                    <div class="d-flex align-items-center gap-3 small text-muted" style="flex-shrink: 0;">
                      <span v-if="image.users?.length" class="text-nowrap">
                        <i class="pi pi-users"></i>
                        {{ image.users.length }} участников
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </template>
          </div>
        </template>

        <!-- Индикатор загрузки дополнительных элементов -->
        <div v-if="isLoadingMore" class="text-center p-2 text-muted small">
          <i class="pi pi-spin pi-spinner me-2"></i>
          <span>Загрузка...</span>
        </div>

        <!-- Сообщение о конце списка -->
        <div v-if="!hasMore && projects.length > 0" class="text-center p-2 text-muted small">
          Все проекты загружены
        </div>
      </div>
    </div>

    <!-- Контекстное меню -->
    <ProjectContextMenu
      :visible="contextMenuVisible"
      :x="contextMenuX"
      :y="contextMenuY"
      @update:visible="contextMenuVisible = $event"
      @edit="handleEdit"
      @create-batch="handleCreateBatch"
      @delete="handleDelete"
    />

    <!-- Модалки -->
    <EditProjectModal
      v-model:visible="isEditModalOpen"
      :project="selectedProject"
    />

    <CreateBatchModal
      v-model:visible="isCreateBatchModalOpen"
      :project="selectedProject"
      @batch-created="handleBatchCreated"
    />

    <!-- Контекстное меню для батчей -->
    <BatchContextMenu
      :visible="batchContextMenuVisible"
      :x="batchContextMenuX"
      :y="batchContextMenuY"
      @update:visible="batchContextMenuVisible = $event"
      @edit="handleBatchEdit"
      @delete="handleBatchDelete"
      @create-images="handleBatchCreateImages"
    />

    <!-- Модалка редактирования батча -->
    <EditBatchModal
      v-model:visible="isEditBatchModalOpen"
      :batch="selectedBatch"
      :project="selectedBatchProject"
      @batch-updated="handleBatchUpdated"
    />

    <!-- Контекстное меню для изображений -->
    <ImageContextMenu
      :visible="imageContextMenuVisible"
      :x="imageContextMenuX"
      :y="imageContextMenuY"
      @update:visible="imageContextMenuVisible = $event"
      @edit="handleImageEdit"
      @delete="handleImageDelete"
    />

    <!-- Модалки для изображений -->
    <CreateImageModal
      v-model:visible="isCreateImageModalOpen"
      :batch="selectedBatch"
      :project="selectedBatchProject"
      @image-created="handleImageCreated"
    />

    <EditImageModal
      v-model:visible="isEditImageModalOpen"
      :image="selectedImage"
      :batch="selectedImageBatch"
      :project="selectedImageProject"
      @image-updated="handleImageUpdated"
    />
  </div>
</template>

<style scoped>
.projects-list {
  background-color: #ffffff;
  border-right: 1px solid rgba(15, 23, 42, 0.08);
  height: 100%;
  max-height: 100%;
  overflow: hidden;
}

.projects-list__search {
  background-color: #f8fafc;
  flex-shrink: 0;
}

.projects-list__filters {
  flex-shrink: 0;
}

.projects-list__container {
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
}

.projects-list__item {
  transition: background-color 0.15s ease;
  height: 6vh;
  display: flex;
  align-items: center;
}

.projects-list__item > div {
  width: 100%;
}

.projects-list__item:hover {
  background-color: #f8fafc;
}

.cursor-pointer {
  cursor: pointer;
}

.projects-list__item--expanded {
  background-color: #f8fafc;
}

.batches-list {
  background-color: #ffffff;
  border-left: 3px solid #e5e7eb;
}

.batch-item {
  background-color: #fafbfc;
  transition: background-color 0.15s ease;
  height: 6vh;
  display: flex;
  align-items: center;
}

.batch-item > div {
  width: 100%;
}

.batch-item:hover {
  background-color: #f3f4f6;
}

.batch-item--expanded {
  background-color: #f1f5f9;
}

.images-list {
  background-color: #ffffff;
  border-left: 3px solid #d1d5db;
}

.image-item {
  background-color: #f9fafb;
  transition: background-color 0.15s ease;
  height: 6vh;
  display: flex;
  align-items: center;
}

.image-item > div {
  width: 100%;
}

.image-item:hover {
  background-color: #f3f4f6;
}
</style>

