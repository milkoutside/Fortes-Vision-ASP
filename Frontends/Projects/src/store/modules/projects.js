import { attachProjectUsers, createProject, deleteProject, fetchProjects, updateProject } from '../../repositories/projectsRepository';

const initialState = () => ({
  items: [],
  isLoading: false,
  isSaving: false,
  isLoadingMore: false,
  error: null,
  pagination: null,
  searchQuery: '',
  hasMore: true,
  currentPage: 1,
  perPage: 100,
});

const mutations = {
  setLoading(state, flag) {
    state.isLoading = flag;
  },
  setLoadingMore(state, flag) {
    state.isLoadingMore = flag;
  },
  setSaving(state, flag) {
    state.isSaving = flag;
  },
  setError(state, error) {
    state.error = error;
  },
  setItems(state, items) {
    state.items = items ?? [];
  },
  appendItems(state, items) {
    state.items = [...state.items, ...(items ?? [])];
  },
  setPagination(state, pagination) {
    state.pagination = pagination;
    state.hasMore = pagination ? pagination.currentPage < pagination.lastPage : false;
    state.currentPage = pagination ? pagination.currentPage : 1;
  },
  setSearchQuery(state, query) {
    state.searchQuery = query ?? '';
  },
  setCurrentPage(state, page) {
    state.currentPage = page;
  },
  upsertItem(state, item) {
    if (!item) return;
    const idx = state.items.findIndex((existing) => existing.id === item.id);
    if (idx === -1) {
      state.items = [item, ...state.items];
    } else {
      state.items = state.items.map((existing) => (existing.id === item.id ? item : existing));
    }
  },
  removeItem(state, id) {
    state.items = state.items.filter((item) => item.id !== id);
  },
  resetState(state) {
    Object.assign(state, initialState());
  },
};

const actions = {
  async fetchAll({ commit, state }, { reset = false, search = null } = {}) {
    if (state.isLoading) return;
    
    const searchQuery = search !== null ? search : state.searchQuery;
    const page = reset ? 1 : state.currentPage;
    
    commit('setLoading', true);
    commit('setError', null);
    
    if (reset) {
      commit('setItems', []);
      commit('setCurrentPage', 1);
      commit('setSearchQuery', searchQuery);
    }
    
    try {
      const result = await fetchProjects({
        page,
        perPage: state.perPage,
        search: searchQuery || null,
      });
      
      if (reset) {
        commit('setItems', result.data);
      } else {
        commit('appendItems', result.data);
      }
      
      commit('setPagination', result.pagination);
      return result;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setLoading', false);
    }
  },
  async loadMore({ commit, state }) {
    if (state.isLoadingMore || !state.hasMore || state.isLoading) return;
    
    commit('setLoadingMore', true);
    commit('setError', null);
    
    try {
      const nextPage = state.currentPage + 1;
      const result = await fetchProjects({
        page: nextPage,
        perPage: state.perPage,
        search: state.searchQuery || null,
      });
      
      commit('appendItems', result.data);
      commit('setPagination', result.pagination);
      return result;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setLoadingMore', false);
    }
  },
  async search({ dispatch }, query) {
    await dispatch('fetchAll', { reset: true, search: query });
  },
  async create({ commit }, payload) {
    commit('setSaving', true);
    commit('setError', null);
    try {
      const project = await createProject(payload);
      commit('upsertItem', project);
      return project;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setSaving', false);
    }
  },
  async attachManagers({ commit }, { projectId, userIds, role = 'project_manager' }) {
    if (!projectId || !userIds?.length) {
      return null;
    }
    commit('setSaving', true);
    commit('setError', null);
    try {
      const project = await attachProjectUsers(projectId, {
        userIds,
        role,
      });
      commit('upsertItem', project);
      return project;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setSaving', false);
    }
  },
  async update({ commit }, { id, payload }) {
    commit('setSaving', true);
    commit('setError', null);
    try {
      const project = await updateProject(id, payload);
      commit('upsertItem', project);
      return project;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setSaving', false);
    }
  },
  async delete({ commit }, id) {
    commit('setSaving', true);
    commit('setError', null);
    try {
      await deleteProject(id);
      commit('removeItem', id);
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setSaving', false);
    }
  },
};

export default {
  namespaced: true,
  state: initialState,
  mutations,
  actions,
};


