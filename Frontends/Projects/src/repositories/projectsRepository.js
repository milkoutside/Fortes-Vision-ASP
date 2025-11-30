import httpClient from './httpClient';

const resource = '/projects';

const unwrap = (response) => response?.data?.data;

export const fetchProjects = async (params = {}) => {
  const { page = 1, perPage = 100, search = null, userIds = null } = params;
  const queryParams = new URLSearchParams();
  queryParams.append('page', page.toString());
  queryParams.append('limit', perPage.toString());
  if (search) {
    queryParams.append('search', search);
  }
  if (userIds && userIds.length > 0) {
    userIds.forEach(id => queryParams.append('userIds', id.toString()));
  }
  const response = await httpClient.get(`${resource}?${queryParams.toString()}`);
  return {
    data: response?.data?.data ?? [],
    pagination: response?.data?.pagination ?? null,
  };
};

export const createProject = async (payload) => {
  const response = await httpClient.post(resource, payload);
  return unwrap(response);
};

export const attachProjectUsers = async (projectId, payload) => {
  const response = await httpClient.post(`${resource}/${projectId}/attach-users`, payload);
  return unwrap(response);
};

export const updateProject = async (projectId, payload) => {
  const response = await httpClient.put(`${resource}/${projectId}`, payload);
  return unwrap(response);
};

export const deleteProject = async (projectId) => {
  await httpClient.delete(`${resource}/${projectId}`);
  return true;
};

export default {
  fetchProjects,
  createProject,
  attachProjectUsers,
  updateProject,
  deleteProject,
};


