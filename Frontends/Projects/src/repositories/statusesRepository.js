import httpClient from './httpClient';

const resource = '/statuses';

const unwrap = (response) => response?.data?.data;

export const fetchStatuses = async () => {
  const response = await httpClient.get(resource);
  return unwrap(response) ?? [];
};

export const createStatus = async (payload) => {
  const response = await httpClient.post(resource, payload);
  return unwrap(response);
};

export const updateStatus = async (id, payload) => {
  const response = await httpClient.put(`${resource}/${id}`, payload);
  return unwrap(response);
};

export const deleteStatus = async (id) => {
  await httpClient.delete(`${resource}/${id}`);
  return true;
};

export default {
  fetchStatuses,
  createStatus,
  updateStatus,
  deleteStatus,
};

