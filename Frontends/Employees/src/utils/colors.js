const palette = [
  '#2563eb',
  '#0ea5e9',
  '#7c3aed',
  '#f97316',
  '#16a34a',
  '#dc2626',
  '#14b8a6',
  '#facc15',
  '#9333ea',
  '#ef4444',
];

const normalizeId = (value) => {
  const num = Number(value);
  if (!Number.isFinite(num)) {
    return 0;
  }
  return Math.abs(Math.trunc(num));
};

export const getProjectColor = (projectId) => {
  if (!palette.length) {
    return '#2563eb';
  }
  const id = normalizeId(projectId);
  return palette[id % palette.length];
};

export default getProjectColor;

