// Base API configuration and helper

const getBaseUrl = (): string => {
  // Use environment variable if provided, else default to empty string for relative proxying in dev
  return import.meta.env.VITE_API_URL || "";
};

export const API_BASE_URL = getBaseUrl();

console.log("VITE_API_URL:", import.meta.env.VITE_API_URL);
console.log("API_BASE_URL:", API_BASE_URL);