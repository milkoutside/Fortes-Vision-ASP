import { createStore } from 'vuex';
import calendar from './modules/calendar';
import workloads from './modules/workloads';
import users from './modules/users';

const store = createStore({
  modules: {
    calendar,
    workloads,
    users,
  },
});

export default store;

