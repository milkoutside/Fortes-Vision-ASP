import httpClient from './httpClient';

const resource = '/projects';
const batchesResource = '/batches';
const batchCalculatorsResource = '/batch-calculators';

const unwrap = (response) => response?.data?.data;

export const getBatches = async (projectId) => {
  const response = await httpClient.get(`${resource}/${projectId}/batches`);
  return unwrap(response) ?? [];
};

export const createBatch = async (projectId, payload) => {
  const response = await httpClient.post(`${resource}/${projectId}/batches`, payload);
  return unwrap(response);
};

export const updateBatch = async (batchId, payload) => {
  const response = await httpClient.put(`${batchesResource}/${batchId}`, payload);
  return unwrap(response);
};

export const deleteBatch = async (projectId, batchId) => {
  await httpClient.delete(`${resource}/${projectId}/batches/${batchId}`);
  return true;
};

export const getBatchCalculator = async (batchId) => {
  const response = await httpClient.get(`${batchCalculatorsResource}/by-batch/${batchId}`);
  return unwrap(response);
};

export const createBatchCalculator = async (payload) => {
  const response = await httpClient.post(batchCalculatorsResource, payload);
  return unwrap(response);
};

export const updateBatchCalculator = async (calculatorId, payload) => {
  const response = await httpClient.put(`${batchCalculatorsResource}/${calculatorId}`, payload);
  return unwrap(response);
};

export const deleteBatchCalculator = async (calculatorId) => {
  await httpClient.delete(`${batchCalculatorsResource}/${calculatorId}`);
  return true;
};

export default {
  getBatches,
  createBatch,
  updateBatch,
  deleteBatch,
  getBatchCalculator,
  createBatchCalculator,
  updateBatchCalculator,
  deleteBatchCalculator,
};

