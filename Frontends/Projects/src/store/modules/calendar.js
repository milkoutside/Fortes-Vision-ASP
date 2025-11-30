// Утилита для форматирования даты в YYYY-MM-DD без сдвига часового пояса
const formatDateLocal = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const normalizeDate = (value) => {
  if (value instanceof Date) {
    return new Date(value.getFullYear(), value.getMonth(), value.getDate());
  }
  if (typeof value === 'string' || typeof value === 'number') {
    const parsed = new Date(value);
    if (!Number.isNaN(parsed.getTime())) {
      return new Date(parsed.getFullYear(), parsed.getMonth(), parsed.getDate());
    }
  }
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
};

const addMonths = (date, delta) => {
  return new Date(date.getFullYear(), date.getMonth() + delta, 1);
};

const formatMonthName = (date) => {
  const formatter = new Intl.DateTimeFormat('ru-RU', { month: 'long' });
  const name = formatter.format(date);
  return name.charAt(0).toUpperCase() + name.slice(1);
};

const buildMonthData = (date) => {
  const year = date.getFullYear();
  const month = date.getMonth();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const dates = [];

  for (let day = 1; day <= daysInMonth; day++) {
    const dayDate = new Date(year, month, day);
    // Используем локальное форматирование вместо toISOString()
    dates.push(formatDateLocal(dayDate));
  }

  return {
    monthName: formatMonthName(date),
    year,
    month,
    dates,
  };
};

const initialState = () => {
  const today = new Date();
  const centerMonth = new Date(today.getFullYear(), today.getMonth(), 1);
  
  return {
    centerMonth: centerMonth.toISOString(),
    today: formatDateLocal(today),
  };
};

const getters = {
  centerMonth: (state) => new Date(state.centerMonth),
  
  today: (state) => state.today,
  
  threeMonthsData: (state) => {
    const base = new Date(state.centerMonth);
    return [-1, 0, 1].map((offset) => buildMonthData(addMonths(base, offset)));
  },
  
  // Все даты за 3 месяца (плоский массив)
  allDates: (state, getters) => {
    return getters.threeMonthsData.flatMap(month => month.dates);
  },
  
  // Ширина блока на все даты
  blockWidth: (state, getters) => {
    return `${getters.allDates.length * 4}vw`;
  },
  
  isWeekend: () => (dateStr) => {
    const [year, month, day] = dateStr.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    const dayOfWeek = date.getDay();
    return dayOfWeek === 0 || dayOfWeek === 6;
  },
  
  isToday: (state) => (dateStr) => {
    return dateStr === state.today;
  },
  
  getWeekday: () => (dateStr) => {
    const [year, month, day] = dateStr.split('-').map(Number);
    const date = new Date(year, month - 1, day);
    return ['Вс', 'Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб'][date.getDay()];
  },
  
  getDayNumber: () => (dateStr) => {
    const [, , day] = dateStr.split('-').map(Number);
    return day;
  },
};

const mutations = {
  setCenterMonth(state, date) {
    const normalized = normalizeDate(date);
    state.centerMonth = new Date(normalized.getFullYear(), normalized.getMonth(), 1).toISOString();
  },
  
  shiftMonths(state, delta) {
    const current = new Date(state.centerMonth);
    const newMonth = addMonths(current, delta);
    state.centerMonth = newMonth.toISOString();
  },
};

const actions = {
  goToPreviousThreeMonths({ commit }) {
    commit('shiftMonths', -3);
  },
  
  goToNextThreeMonths({ commit }) {
    commit('shiftMonths', 3);
  },
  
  goToToday({ commit }) {
    const today = new Date();
    commit('setCenterMonth', today);
  },
  
  setMonth({ commit }, date) {
    commit('setCenterMonth', date);
  },
};

export default {
  namespaced: true,
  state: initialState,
  getters,
  mutations,
  actions,
};

