import {
  fetchStatuses,
  createStatus,
  updateStatus,
  deleteStatus,
} from '../../repositories/statusesRepository';

const initialState = () => ({
  items: [],
  isLoading: false,
  isSaving: false,
  hasLoaded: false,
  error: null,
  selectedStatusId: null,
});

const mutations = {
  setLoading(state, flag) {
    state.isLoading = flag;
  },
  setSaving(state, flag) {
    state.isSaving = flag;
  },
  setItems(state, items) {
    state.items = items;
  },
  setError(state, error) {
    state.error = error;
  },
  setHasLoaded(state, flag) {
    state.hasLoaded = flag;
  },
  addItem(state, item) {
    state.items = [...state.items, item];
  },
  replaceItem(state, item) {
    state.items = state.items.map((existing) =>
      existing.id === item.id ? item : existing,
    );
  },
  removeItem(state, id) {
    state.items = state.items.filter((item) => item.id !== id);
    if (state.selectedStatusId === id) {
      state.selectedStatusId = null;
    }
  },
  setSelectedStatus(state, id) {
    state.selectedStatusId = id;
  },
  resetState(state) {
    Object.assign(state, initialState());
  },
};

const actions = {
  async fetchAll({ commit, state }) {
    if (state.isLoading) return;
    commit('setLoading', true);
    commit('setError', null);
    try {
      const items = await fetchStatuses();
      commit('setItems', items);
      commit('setHasLoaded', true);
      if (!items.length) {
        commit('setSelectedStatus', null);
      } else if (
        state.selectedStatusId &&
        !items.some((item) => item.id === state.selectedStatusId)
      ) {
        commit('setSelectedStatus', null);
      }
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setLoading', false);
    }
  },
  async create({ commit }, payload) {
    commit('setSaving', true);
    commit('setError', null);
    try {
      const created = await createStatus(payload);
      commit('addItem', created);
      return created;
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
      const updated = await updateStatus(id, payload);
      commit('replaceItem', updated);
      return updated;
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
      await deleteStatus(id);
      commit('removeItem', id);
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setSaving', false);
    }
  },
  select({ commit }, id) {
    commit('setSelectedStatus', id);
  },
};

export default {
  namespaced: true,
  state: initialState,
  mutations,
  actions,
};

