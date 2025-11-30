import axios from 'axios';

const defaultBaseURL =
  import.meta.env.VITE_EMPLOYEES_API_BASE_URL ??
  import.meta.env.VITE_API_BASE_URL ??
  '/api';

const httpClient = axios.create({
  baseURL: defaultBaseURL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      const message = error.response.data?.message || error.message;
      return Promise.reject(new Error(message));
    }
    return Promise.reject(error);
  },
);

export default httpClient;

