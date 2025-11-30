import { createStore } from 'vuex';
import statuses from './modules/statuses';
import users from './modules/users';
import projects from './modules/projects';
import calendar from './modules/calendar';
import tasks from './modules/tasks';

const store = createStore({
  modules: {
    statuses,
    users,
    projects,
    calendar,
    tasks,
  },
});

export default store;

