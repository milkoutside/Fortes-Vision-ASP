import httpClient from './httpClient';

const buildResourcePath = ({ projectId, batchId, imageId }) =>
  `/projects/${projectId}/batches/${batchId}/images/${imageId}/tasks`;

const unwrap = (response) => response?.data?.data ?? [];

export const fetchTasks = async (scope, params = {}) => {
  const response = await httpClient.get(buildResourcePath(scope), { params });
  return unwrap(response);
};

export const bulkCreateTasks = async (scope, tasks) => {
  if (!tasks?.length) {
    return [];
  }
  const response = await httpClient.post(`${buildResourcePath(scope)}/bulk`, {
    tasks,
  });
  return unwrap(response);
};

export const bulkUpdateTasks = async (scope, tasks) => {
  if (!tasks?.length) {
    return [];
  }
  const response = await httpClient.put(`${buildResourcePath(scope)}/bulk`, {
    tasks,
  });
  return unwrap(response);
};

export const bulkDeleteTasks = async (scope, taskIds) => {
  if (!taskIds?.length) {
    return;
  }
  await httpClient.delete(`${buildResourcePath(scope)}/bulk`, {
    data: { taskIds },
  });
};

export default {
  fetchTasks,
  bulkCreateTasks,
  bulkUpdateTasks,
  bulkDeleteTasks,
};








