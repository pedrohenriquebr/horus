import { Source } from "../db/schema";

// interfaces.ts
export interface ProblemDetails {
    type?: string;
    title?: string;
    status?: number;
    detail?: string;
    instance?: string;
    [key: string]: any;
  }


  // Para /api/v1/generate-text (POST)
export interface GenerateTextQuery {
  prompt?: string;
  userInfo?: { [key: string]: string };
  imagePath?: string;
  audioPath?: string;
  systemInstructions?: string;
}

// Para /api/v1/documents/add-document (POST)
export interface AddDocumentCommandRequest {
  projectId?: string;
  name?: string;
  sourceUri?: string;
  fileContent?: File; // binary
  chunkingStrategy?: string;
}

export interface ValidateCredentialsResponse {
  isValid: boolean;
  userId?: string;
  email?: string;
  name?: string;
}

// Para /api/v1/users/register (POST)
export interface RegisterUserCommand {
  email?: string;
  password?: string;
  name?: string;
}

// Para /api/v1/projects (POST)
export interface CreateProjectCommand {
  id: string;
  name: string;
  systemPrompt: string;
  metadata?: { [key: string]: any };
}

// Para /api/v1/projects/{id} (PUT)
export interface UpdateProjectCommand {
  name: string;
  systemPrompt: string;
  metadata?: { [key: string]: any };
}

// Para /api/v1/chatsessions (POST)
export interface CreateChatSessionCommand {
  id: string;
  projectId: string;
  title: string;
  endedAt: string;
  metadata?: { [key: string]: any };
}

// Para /api/v1/chatsessions/{id} (PUT)
export interface UpdateChatSessionCommand {
  projectId: string;
  title: string;
  endedAt: string;
  metadata?: { [key: string]: any };
}

// Para /api/v1/chatmessages (POST)
export interface CreateChatMessageCommand {
  id: string;
  sessionId: string;
  role: string;
  content: string;
  queryEmbedding: number[];
  referenceChunkIds: string[];
  metadata?: { [key: string]: any };
}

// Para /api/v1/chatmessages/{id} (PUT)
export interface UpdateChatMessageCommand {
  sessionId: string;
  role: string;
  content: string;
  queryEmbedding: number[];
  referenceChunkIds: string[];
  metadata?: { [key: string]: any };
}

// Para /api/v1/generate-text (POST)
export interface GenerateTextQueryResponse {
  text?: string;
  sources?: Source[];
}

// Para /api/v1/projects (GET)
export interface GetAllProjectResponse {
  items: Project[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

// Para /api/v1/projects/{id} (GET)
export interface GetProjectByIdResponse extends Project {}

// Para /api/v1/chatsessions (GET)
export interface GetAllChatSessionResponse {
  items: ChatSession[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

export interface GetChatSessionsByUserIdQueryResponse {
  items: ChatSession[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

// Para /api/v1/chatsessions/{id} (GET)
export interface GetChatSessionByIdResponse extends ChatSession {}

// Para /api/v1/chatmessages (GET)
export interface GetAllChatMessageResponse {
  items: ChatMessage[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

// Para /api/v1/chatmessages/{id} (GET)
export interface GetChatMessageByIdResponse extends ChatMessage {}

// Para /api/v1/users (GET)
export interface GetAllUserResponse {
  items: User[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

// Para /api/v1/users/{id} (GET)
export interface GetUserByIdResponse extends User {}

// Para /api/v1/documents (GET)
export interface GetAllDocumentResponse {
  items: Document[];
  total: number;
  page: number;
  totalPages: number;
  pageSize: number;
}

// Para /api/v1/documents/{id} (GET)
export interface GetDocumentByIdResponse extends Document {}


export interface Project {
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  name?: string;
  systemPrompt?: string;
  documents?: Document[];
  chatSessions?: ChatSession[];
}

export interface ChatSession {
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  projectId?: string;
  title?: string;
  endedAt?: string;
  project: Project;
  messages?: ChatMessage[];
}

export interface ChatMessage {
  sources?: Source[];
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  sessionId: string;
  role?: string;
  content?: string;
  queryEmbedding: number[];
  referenceChunkIds?: string[];
}

export interface User {
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  email?: string;
  name?: string;
  passwordHash?: string;
  salt?: string;
}

export interface Document {
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  projectId?: string;
  sourceUri?: string;
  rawContent?: string;
  processedContent?: string;
  checksum?: string;
  statusId: number;
  errorCode?: string;
  errorMessage?: string;
  retryCount: number;
  lastProcessedAt?: string;
  chunkingStrategy?: string;
  chunks?: DocumentChunk[];
}

export interface DocumentChunk {
  createdAt: string;
  updatedAt?: string;
  metadata?: { [key: string]: any };
  id: string;
  documentId: string;
  chunkNumber: number;
  content?: string;
  tokenCount: number;
  startOffset: number;
  endOffset: number;
  statusId?: number;
  processingError?: string;
  embeddingError?: string;
  searchVector?: Lexeme[];
  languageCode?: string;
  embedding: ChunkEmbedding;
}

export interface ChunkEmbedding {
  createdAt: string;
  updatedAt?: string;
  chunkId: string;
  modelId: number;
  embedding: number[];
  embeddingModel: EmbeddingModel;
}

export interface EmbeddingModel {
  createdAt: string;
  updatedAt?: string;
  id: number;
  name?: string;
  version?: string;
  dimensions: number;
}

export interface Lexeme {
  text?: string;
  count: number;
}



export interface SingleReadOnlyMemory {
  length: number;
  isEmpty: boolean;
  span: SingleReadOnlySpan;
}

export interface SingleReadOnlySpan {
  length: number;
  isEmpty: boolean;
}