import * as React from "react";

interface DropzoneProps {
  onFileSelected: (file: File) => void;
}

export const Dropzone: React.FC<DropzoneProps> = ({ onFileSelected }) => {
  const [dragActive, setDragActive] = React.useState(false);
  const fileInputRef = React.useRef<HTMLInputElement>(null);

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      onFileSelected(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      onFileSelected(e.target.files[0]);
    }
  };

  const onButtonClick = () => {
    fileInputRef.current?.click();
  };

  return (
    <div 
      className={`dropzone ${dragActive ? "active" : ""}`}
      onDragEnter={handleDrag}
      onDragLeave={handleDrag}
      onDragOver={handleDrag}
      onDrop={handleDrop}
      onClick={onButtonClick}
      style={{ marginBottom: "20px" }}
    >
      <input 
        type="file" 
        ref={fileInputRef}
        onChange={handleFileChange}
        style={{ display: "none" }}
      />
      
      {/* Minimalist Upload Cloud-Arrow icon */}
      <svg 
        width="50" 
        height="50" 
        viewBox="0 0 24 24" 
        fill="none" 
        stroke="#818cf8" 
        strokeWidth="1.5" 
        strokeLinecap="round" 
        strokeLinejoin="round"
        style={{ marginBottom: "16px" }}
      >
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
        <polyline points="17 8 12 3 7 8" />
        <line x1="12" y1="3" x2="12" y2="15" />
      </svg>

      <div className="dropzone-text" style={{ fontSize: "15px", color: "var(--text)" }}>
        Drag and drop file here
      </div>
      <div className="dropzone-subtext" style={{ margin: "8px 0", color: "#a5b4fc" }}>
        -OR-
      </div>
      <button 
        type="button" 
        className="btn btn-primary"
        style={{ background: "#1e0094", borderRadius: "10px", padding: "10px 24px", fontSize: "14px" }}
        onClick={(e) => { e.stopPropagation(); onButtonClick(); }}
      >
        Browse Files
      </button>
    </div>
  );
};
