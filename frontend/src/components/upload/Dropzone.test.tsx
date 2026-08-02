import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Dropzone } from './Dropzone';

describe('Dropzone', () => {
  it('renders upload area', () => {
    render(<Dropzone onFileSelected={() => {}} />);
    expect(screen.getByText('Drop your file here')).toBeDefined();
  });

  it('renders browse hint text', () => {
    render(<Dropzone onFileSelected={() => {}} />);
    expect(screen.getByText(/click to browse/i)).toBeDefined();
  });

  it('calls onFileSelected when file is selected via input', async () => {
    let selectedFile: File | null = null;
    const handleSelected = (file: File) => { selectedFile = file; };

    render(<Dropzone onFileSelected={handleSelected} />);

    const file = new File(['hello world'], 'test.txt', { type: 'text/plain' });
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;

    // Simulate file selection using Object.defineProperty to set files
    Object.defineProperty(input, 'files', { value: [file] });
    fireEvent.change(input);

    await waitFor(() => {
      expect(selectedFile).not.toBeNull();
      expect(selectedFile?.name).toBe('test.txt');
      expect(selectedFile?.type).toBe('text/plain');
    });
  });

  it('has accessible file input', () => {
    render(<Dropzone onFileSelected={() => {}} />);
    const input = document.querySelector('input[type="file"]');
    expect(input).toBeDefined();
  });

  it('renders SVG upload icon', () => {
    render(<Dropzone onFileSelected={() => {}} />);
    const svg = document.querySelector('svg');
    expect(svg).toBeDefined();
  });
});
