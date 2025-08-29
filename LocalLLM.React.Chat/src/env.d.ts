// /// <reference types="vite/client" />

// (Optional) make TS aware of your var
// interface ImportMetaEnv {
//   readonly VITE_API_BASE?: string;
// }
// interface ImportMeta {
//   readonly env: ImportMetaEnv;
// }

/// <reference types="vite/client" />
interface ImportMetaEnv { readonly VITE_API_BASE?: string }
interface ImportMeta { readonly env: ImportMetaEnv }
