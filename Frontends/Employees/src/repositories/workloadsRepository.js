import httpClient from './httpClient';

const unwrap = (response) => response?.data?.data ?? {
  users: [],
  from: null,
  to: null,
};

export const fetchWorkloads = async (params = {}) => {
  const response = await httpClient.get('/workloads/users', { params });
  return unwrap(response);
};

export default {
  fetchWorkloads,
};

