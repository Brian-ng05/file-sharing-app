import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Loading } from '../../components/common/Loading';

describe('Loading', () => {
  it('renders with default message', () => {
    render(<Loading />);
    expect(screen.getByText('Checking file availability…')).toBeDefined();
  });

  it('renders with custom message', () => {
    render(<Loading message="Uploading file..." />);
    expect(screen.getByText('Uploading file...')).toBeDefined();
  });

  it('renders spinner container', () => {
    render(<Loading />);
    const spinner = document.querySelector('.spinner-wrapper');
    expect(spinner).toBeDefined();
  });

  it('renders with correct card class', () => {
    render(<Loading />);
    const card = document.querySelector('.card');
    expect(card).toBeDefined();
  });
});
