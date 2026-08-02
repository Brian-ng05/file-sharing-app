import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { MemoryRouter } from 'react-router-dom';

describe('ErrorMessage', () => {
  function renderWithRouter(ui: React.ReactElement) {
    return render(<MemoryRouter>{ui}</MemoryRouter>);
  }

  // ──────────────────────────────────────────────
  // RENDERING
  // ──────────────────────────────────────────────

  it('renders with a message', () => {
    renderWithRouter(<ErrorMessage message="Something went wrong" />);
    expect(screen.getByText('Something went wrong')).toBeDefined();
  });

  it('renders with custom title', () => {
    renderWithRouter(<ErrorMessage title="Custom Title" message="Error details" />);
    expect(screen.getByText('Custom Title')).toBeDefined();
  });

  // ──────────────────────────────────────────────
  // TITLE INFERENCE
  // ──────────────────────────────────────────────

  it('infers expired title from message containing "expired"', () => {
    renderWithRouter(<ErrorMessage message="File has expired" />);
    expect(screen.getByText('File Expired / Unavailable')).toBeDefined();
  });

  it('infers not found title from message containing "not found"', () => {
    renderWithRouter(<ErrorMessage message="File not found" />);
    expect(screen.getByText('File Not Found')).toBeDefined();
  });

  it('infers password title from message containing "password"', () => {
    renderWithRouter(<ErrorMessage message="Password is required" />);
    expect(screen.getByText('Password Required')).toBeDefined();
  });

  it('infers download limit title from message containing "limit"', () => {
    renderWithRouter(<ErrorMessage message="Download limit reached" />);
    expect(screen.getByText('Download Limit Reached')).toBeDefined();
  });

  it('infers service unavailable title from message containing "storage"', () => {
    renderWithRouter(<ErrorMessage message="Storage service error" />);
    expect(screen.getByText('Service Unavailable')).toBeDefined();
  });

  it('infers network error title from message containing "network"', () => {
    renderWithRouter(<ErrorMessage message="Network connection failed" />);
    expect(screen.getByText('Network Error')).toBeDefined();
  });

  it('falls back to default title for unknown messages', () => {
    renderWithRouter(<ErrorMessage message="Unknown error occurred" />);
    expect(screen.getByText('Access Denied / Expired')).toBeDefined();
  });

  // ──────────────────────────────────────────────
  // BUTTON VISIBILITY
  // ──────────────────────────────────────────────

  it('shows upload button by default', () => {
    renderWithRouter(<ErrorMessage message="Error" />);
    expect(screen.getByText('Upload a File')).toBeDefined();
  });

  it('shows history button by default', () => {
    renderWithRouter(<ErrorMessage message="Error" />);
    expect(screen.getByText('Go to History')).toBeDefined();
  });

  it('hides upload button when showUploadButton is false', () => {
    renderWithRouter(
      <ErrorMessage message="Error" showUploadButton={false} />
    );
    expect(screen.queryByText('Upload a File')).toBeNull();
  });

  it('hides history button when showHistoryButton is false', () => {
    renderWithRouter(
      <ErrorMessage message="Error" showHistoryButton={false} />
    );
    expect(screen.queryByText('Go to History')).toBeNull();
  });
});
