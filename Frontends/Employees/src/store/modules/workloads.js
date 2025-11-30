import { fetchWorkloads } from '../../repositories/workloadsRepository';

const initialState = () => ({
  items: [],
  range: {
    from: null,
    to: null,
  },
  isLoading: false,
  error: null,
  lastLoadedAt: null,
});

const mutations = {
  setLoading(state, isLoading) {
    state.isLoading = isLoading;
  },
  setError(state, error) {
    state.error = error;
  },
  setData(state, payload) {
    state.items = payload?.users ?? [];
    state.range = {
      from: payload?.from ?? null,
      to: payload?.to ?? null,
    };
    state.error = null;
    state.lastLoadedAt = Date.now();
  },
  resetState(state) {
    Object.assign(state, initialState());
  },
};

const actions = {
  async fetchWorkloads({ commit }, params = {}) {
    commit('setLoading', true);
    commit('setError', null);
    try {
      const data = await fetchWorkloads(params);
      commit('setData', data);
      return data;
    } catch (error) {
      commit('setError', error);
      throw error;
    } finally {
      commit('setLoading', false);
    }
  },
};

const getters = {
  users: (state) => state.items,
  range: (state) => state.range,
  isLoading: (state) => state.isLoading,
  error: (state) => state.error,
};

export default {
  namespaced: true,
  state: initialState,
  mutations,
  actions,
  getters,
};

