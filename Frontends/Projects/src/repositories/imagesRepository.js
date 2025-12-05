import httpClient from './httpClient';

const unwrap = (response) => response?.data?.data;

export const getImages = async (projectId, batchId) => {
  const response = await httpClient.get(`/projects/${projectId}/batches/${batchId}/images`);
  return unwrap(response) ?? [];
};

export const createImage = async (projectId, batchId, payload) => {
  const response = await httpClient.post(`/projects/${projectId}/batches/${batchId}/images`, payload);
  return unwrap(response);
};

export const updateImage = async (projectId, batchId, imageId, payload) => {
  const response = await httpClient.put(`/projects/${projectId}/batches/${batchId}/images/${imageId}`, payload);
  return unwrap(response);
};

export const deleteImage = async (projectId, batchId, imageId) => {
  await httpClient.delete(`/projects/${projectId}/batches/${batchId}/images/${imageId}`);
  return true;
};

export default {
  getImages,
  createImage,
  updateImage,
  deleteImage,
};









