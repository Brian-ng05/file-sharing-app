// Base API configuration and helper

const getBaseUrl = (): string => {
  // Use environment variable if provided, else default to empty string for relative proxying in dev
  return import.meta.env.VITE_API_URL || "";
};

export const API_BASE_URL = getBaseUrl();

// We will default to Mock Mode if VITE_USE_MOCK_API is 'true' or if we are in development
// and want to work independently of the backend API (Member 1).
export const isMockMode = (): boolean => {
  const mockOverride = localStorage.getItem("use_mock");
  if (mockOverride !== null) {
    return mockOverride === "true";
  }
  
  return (
    import.meta.env.VITE_USE_MOCK_API === "true" ||
    !import.meta.env.VITE_API_URL ||
    import.meta.env.DEV
  );
};

export const setMockMode = (value: boolean): void => {
  localStorage.setItem("use_mock", String(value));
};
