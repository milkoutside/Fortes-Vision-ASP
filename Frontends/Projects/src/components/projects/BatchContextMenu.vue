<script setup>
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';

const props = defineProps({
  visible: {
    type: Boolean,
    default: false,
  },
  x: {
    type: Number,
    default: 0,
  },
  y: {
    type: Number,
    default: 0,
  },
});

const emit = defineEmits(['update:visible', 'edit', 'delete', 'create-images']);

const menuRef = ref(null);
const menuStyle = ref({});

const updatePosition = async () => {
  if (!menuRef.value || !props.visible) return;
  
  await nextTick();
  
  const menu = menuRef.value;
  const menuRect = menu.getBoundingClientRect();
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  
  let left = props.x;
  let top = props.y;
  
  // Проверяем правую границу
  if (left + menuRect.width > viewportWidth) {
    left = viewportWidth - menuRect.width - 10;
  }
  
  // Проверяем левую границу
  if (left < 10) {
    left = 10;
  }
  
  // Проверяем нижнюю границу
  if (top + menuRect.height > viewportHeight) {
    top = viewportHeight - menuRect.height - 10;
  }
  
  // Проверяем верхнюю границу
  if (top < 10) {
    top = 10;
  }
  
  menuStyle.value = {
    position: 'fixed',
    left: `${left}px`,
    top: `${top}px`,
    zIndex: 1000,
  };
};

const handleClickOutside = (event) => {
  if (menuRef.value && !menuRef.value.contains(event.target)) {
    emit('update:visible', false);
  }
};

const handleEdit = () => {
  emit('edit');
  emit('update:visible', false);
};

const handleDelete = () => {
  emit('delete');
  emit('update:visible', false);
};

const handleCreateImages = () => {
  emit('create-images');
  emit('update:visible', false);
};

watch(() => props.visible, (visible) => {
  if (visible) {
    setTimeout(() => updatePosition(), 0);
    document.addEventListener('click', handleClickOutside);
    document.addEventListener('contextmenu', handleClickOutside);
  } else {
    document.removeEventListener('click', handleClickOutside);
    document.removeEventListener('contextmenu', handleClickOutside);
  }
});

watch(() => [props.x, props.y], () => {
  if (props.visible) {
    setTimeout(() => updatePosition(), 0);
  }
});

onMounted(() => {
  if (props.visible) {
    setTimeout(() => updatePosition(), 0);
  }
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  document.removeEventListener('contextmenu', handleClickOutside);
});
</script>

<template>
  <div
    v-if="visible"
    ref="menuRef"
    class="batch-context-menu"
    :style="menuStyle"
    @click.stop
  >
    <div class="batch-context-menu__item" @click="handleEdit">
      <i class="pi pi-info-circle me-2"></i>
      <span>Батч инфо</span>
    </div>
    <div class="batch-context-menu__item" @click="handleCreateImages">
      <i class="pi pi-plus me-2"></i>
      <span>Создать изображения</span>
    </div>
    <div class="batch-context-menu__divider"></div>
    <div class="batch-context-menu__item batch-context-menu__item--danger" @click="handleDelete">
      <i class="pi pi-trash me-2"></i>
      <span>Удалить батч</span>
    </div>
  </div>
</template>

<style scoped>
.batch-context-menu {
  background-color: #ffffff;
  border: 1px solid rgba(15, 23, 42, 0.1);
  border-radius: 8px;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
  min-width: 200px;
  padding: 4px 0;
}

.batch-context-menu__item {
  padding: 10px 16px;
  cursor: pointer;
  display: flex;
  align-items: center;
  color: #1f2937;
  font-size: 14px;
  transition: background-color 0.15s ease;
  user-select: none;
}

.batch-context-menu__item:hover {
  background-color: #f3f4f6;
}

.batch-context-menu__item--danger {
  color: #dc2626;
}

.batch-context-menu__item--danger:hover {
  background-color: #fee2e2;
}

.batch-context-menu__divider {
  height: 1px;
  background-color: rgba(15, 23, 42, 0.1);
  margin: 4px 0;
}
</style>









