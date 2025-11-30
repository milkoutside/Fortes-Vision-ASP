import httpClient from './httpClient';

const resource = '/users';

const defaultPagination = {
  currentPage: 1,
  perPage: 10,
  total: 0,
  lastPage: 1,
};

export const fetchUsers = async (params = {}) => {
  const response = await httpClient.get(resource, {
    params: {
      page: 1,
      limit: 10,
      ...params,
    },
  });
  return {
    items: response?.data?.data ?? [],
    pagination: response?.data?.pagination ?? defaultPagination,
  };
};

export const createUser = async (payload) => {
  const response = await httpClient.post(resource, payload);
  return response?.data?.data;
};

export const updateUser = async (id, payload) => {
  const response = await httpClient.put(`${resource}/${id}`, payload);
  return response?.data?.data;
};

export const deleteUser = async (id) => {
  await httpClient.delete(`${resource}/${id}`);
  return true;
};

export default {
  fetchUsers,
  createUser,
  updateUser,
  deleteUser,
};

