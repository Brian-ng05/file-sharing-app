export class CryptoService {
  private static ITERATIONS = 100000;
  private static KEY_LEN = 256;

  private static async deriveKey(password: string, salt: Uint8Array): Promise<CryptoKey> {
    const encoder = new TextEncoder();
    const passwordBuffer = encoder.encode(password);
    
    const baseKey = await window.crypto.subtle.importKey(
      "raw",
      passwordBuffer,
      "PBKDF2",
      false,
      ["deriveKey"]
    );
    
    return await window.crypto.subtle.deriveKey(
      {
        name: "PBKDF2",
        salt: salt,
        iterations: this.ITERATIONS,
        hash: "SHA-256"
      },
      baseKey,
      {
        name: "AES-GCM",
        length: this.KEY_LEN
      },
      false,
      ["encrypt", "decrypt"]
    );
  }

  public static async encryptFile(file: File | Blob, password: string): Promise<Blob> {
    const arrayBuffer = await file.arrayBuffer();
    
    // Generate salt and IV
    const salt = window.crypto.getRandomValues(new Uint8Array(16));
    const iv = window.crypto.getRandomValues(new Uint8Array(12));
    
    // Derive key
    const key = await this.deriveKey(password, salt);
    
    // Encrypt content
    const ciphertext = await window.crypto.subtle.encrypt(
      {
        name: "AES-GCM",
        iv: iv
      },
      key,
      arrayBuffer
    );
    
    // Structure: 16 bytes salt | 12 bytes iv | ciphertext
    const resultBuffer = new Uint8Array(salt.byteLength + iv.byteLength + ciphertext.byteLength);
    resultBuffer.set(salt, 0);
    resultBuffer.set(iv, salt.byteLength);
    resultBuffer.set(new Uint8Array(ciphertext), salt.byteLength + iv.byteLength);
    
    return new Blob([resultBuffer], { type: "application/octet-stream" });
  }

  public static async decryptFile(encryptedBlob: Blob, password: string): Promise<ArrayBuffer> {
    const arrayBuffer = await encryptedBlob.arrayBuffer();
    
    if (arrayBuffer.byteLength < 28) {
      throw new Error("Invalid encrypted file data.");
    }
    
    // Extract salt, IV and ciphertext
    const salt = new Uint8Array(arrayBuffer, 0, 16);
    const iv = new Uint8Array(arrayBuffer, 16, 12);
    const ciphertext = new Uint8Array(arrayBuffer, 28);
    
    // Derive key
    const key = await this.deriveKey(password, salt);
    
    // Decrypt content
    try {
      return await window.crypto.subtle.decrypt(
        {
          name: "AES-GCM",
          iv: iv
        },
        key,
        ciphertext
      );
    } catch (e) {
      throw new Error("Incorrect password or corrupted file.");
    }
  }
}
