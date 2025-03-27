// api-service.ts
import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import {
  ProblemDetails,
  GenerateTextQuery,
  GenerateTextQueryResponse,
  AddDocumentCommandRequest,
  RegisterUserCommand,
  CreateProjectCommand,
  UpdateProjectCommand,
  GetAllProjectResponse,
  GetProjectByIdResponse,
  CreateChatSessionCommand,
  UpdateChatSessionCommand,
  GetAllChatSessionResponse,
  GetChatSessionByIdResponse,
  CreateChatMessageCommand,
  GetAllChatMessageResponse,
  GetAllDocumentResponse,
  GetAllUserResponse,
  GetChatMessageByIdResponse,
  GetDocumentByIdResponse,
  GetUserByIdResponse,
  UpdateChatMessageCommand,
  User,
  ValidateCredentialsResponse,
  GetChatSessionsByUserIdQueryResponse,
} from './interfaces';

export class HorusApiService {

  private readonly axiosInstance: AxiosInstance;

  constructor(baseURL: string) {

    console.info(`Initializing HorusApiService with baseURL: ${baseURL}`);
    this.axiosInstance = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.axiosInstance.interceptors.response.use(
        (response) => {
            const {config} = response;
            const url = config?.url || '';
            
            console.info('Request URL:', url);
            console.info('Request Method:', config?.method);
            console.info('Request Data:', config?.data);
            console.info('Request Headers:', config?.headers);
            console.info('Response Status:', response.status);
            console.info('Response Data:', response.data);
            
          // Handle successful responses (if needed)
          return response;
        },
        (error: AxiosError) => {
          if (axios.isAxiosError(error) && error.response) {
            const { config } = error;
            const url = config?.url || '';

            console.error('Request URL:', url);
            console.error('Request Method:', config?.method);
            console.error('Request Data:', config?.data);
            console.error('Request Headers:', config?.headers);
            console.error('Response Status:', error.response.status);
            console.error('Response Data:', error.response.data);
  
            // Apply logic only to specific routes
            if (url.startsWith('/users/get-by-email/') || url.startsWith('/chatmessages/') || url.startsWith('/chatsessions/')) {
              if (error.response.status === 404) {
                console.log('User not found');
                return Promise.reject(error); // Custom handling: return null for 404
              }
            }
  
            // Default error handling for other routes
            const problemDetails = error.response.data as ProblemDetails;
            console.error('Error Details:', problemDetails.detail);
            console.error('Error message:', error.message)
            console.error('Error response:', error.response.data)
            return Promise.reject(problemDetails.detail || 'An error occurred');
          }
          return Promise.reject(error);
        }
      );
      
  }

  // POST /api/v1/generate-text
  async generateText(query: GenerateTextQuery): Promise<GenerateTextQueryResponse> {
    const response = await this.axiosInstance.post<GenerateTextQueryResponse>('/generate-text', query);
    return response.data;
  }

  // Documents
  async addDocument(dto: AddDocumentCommandRequest): Promise<void> {
    const formData = new FormData();
    formData.append('name', dto.name);
    formData.append('sourceUri', dto.sourceUri);
    formData.append('chunkingStrategy', dto.chunkingStrategy);
    formData.append('fileContent', dto.fileContent);
    formData.append('projectId', dto.projectId);

    await this.axiosInstance.post('/documents/add-document', formData);
  }


  async getUserByEmail(email: string): Promise<User | null> {
    try {
      const response = await this.axiosInstance.get<User>(`/users/get-by-email/${encodeURIComponent(email)}`);
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Retorna null se o usuário não for encontrado
      }
      throw error; // Propaga outros erros para o interceptor ou chamador
    }
  }


  async validateUser(email: string, password: string): Promise<ValidateCredentialsResponse> {
    try {
      const response = await this.axiosInstance.post(`/auth/validate/`, { email, password });
      return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Retorna null se o usuário não for encontrado
      }
      throw error; // Propaga outros erros para o interceptor ou chamador
    }
  }

  // POST /api/v1/projects
  async createProject(command: CreateProjectCommand): Promise<void> {
    await this.axiosInstance.post('/projects', command);
  }

  // POST /api/v1/users/register
  async registerUser(command: RegisterUserCommand): Promise<void> {
    await this.axiosInstance.post('/users/register', command);
  }

  // GET /api/v1/projects
  async getAllProjects(params: {
    sort: string;
    offset: number;
    pageSize: number;
    filter?: string;
  }): Promise<GetAllProjectResponse> {
    const response = await this.axiosInstance.get<GetAllProjectResponse>('/projects', { params });
    return response.data;
  }

  // GET /api/v1/projects/{id}
  async getProject(id: string): Promise<GetProjectByIdResponse> {
    const response = await this.axiosInstance.get<GetProjectByIdResponse>(`/projects/${id}`);
    return response.data;
  }

  // PUT /api/v1/projects/{id}
  async updateProject(id: string, command: UpdateProjectCommand): Promise<void> {
    await this.axiosInstance.put(`/projects/${id}`, command);
  }

  // DELETE /api/v1/projects/{id}
  async deleteProject(id: string): Promise<void> {
    await this.axiosInstance.delete(`/projects/${id}`);
  }

  // POST /api/v1/chatsessions
  async createChatSession(command: CreateChatSessionCommand): Promise<void> {
    await this.axiosInstance.post('/chatsessions', command);
  }

  // GET /api/v1/chatsessions
  async getAllChatSessions(params: {
    sort: string;
    offset: number;
    pageSize: number;
    filter?: string;
  }): Promise<GetAllChatSessionResponse> {
    const response = await this.axiosInstance.get<GetAllChatSessionResponse>('/chatsessions', { params });
    return response.data;
  }


    // GET /api/v1/chatsessions/get-by-user-id
    async getChatSessiosByUserId(userId, params: {
      offset: number;
      pageSize: number;
    }): Promise<GetChatSessionsByUserIdQueryResponse> {
      const response = await this.axiosInstance.get<GetChatSessionsByUserIdQueryResponse>('/chatsessions/get-by-user-id/'+userId, { params });
      return response.data;
    }

  // GET /api/v1/chatsessions/{id}
  async getChatSession(id: string): Promise<GetChatSessionByIdResponse> {
    try{
    const response = await this.axiosInstance.get<GetChatSessionByIdResponse>(`/chatsessions/${id}`);
    return response.data;
    }catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Retorna null se o chatte nao for encontrado
      }
      throw error; // Propaga outros erros para o interceptor ou chamador
    }
  }

  // PUT /api/v1/chatsessions/{id}
  async updateChatSession(id: string, command: UpdateChatSessionCommand): Promise<void> {
    await this.axiosInstance.put(`/chatsessions/${id}`, command);
  }

  // DELETE /api/v1/chatsessions/{id}
  async deleteChatSession(id: string): Promise<void> {
    await this.axiosInstance.delete(`/chatsessions/${id}`);
  }

  // POST /api/v1/chatmessages
  async createChatMessage(command: CreateChatMessageCommand): Promise<void> {
    await this.axiosInstance.post('/chatmessages', command);
  }

  // GET /api/v1/chatmessages
  async getAllChatMessages(params: {
    sort: string;
    offset: number;
    pageSize: number;
    filter?: string;
  }): Promise<GetAllChatMessageResponse> {
    const response = await this.axiosInstance.get<GetAllChatMessageResponse>('/chatmessages', { params });
    return response.data;
  }

  // GET /api/v1/chatmessages/{id}
  async getChatMessage(id: string): Promise<GetChatMessageByIdResponse> {
    try {
      const response = await this.axiosInstance.get<GetChatMessageByIdResponse>(`/chatmessages/${id}`);
    return response.data;
    } catch (error) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null; // Retorna null se o usuário não for encontrado
      }
      throw error; // Propaga outros erros para o interceptor ou chamador
    }
  }

  // PUT /api/v1/chatmessages/{id}
  async updateChatMessage(id: string, command: UpdateChatMessageCommand): Promise<void> {
    await this.axiosInstance.put(`/chatmessages/${id}`, command);
  }

  // DELETE /api/v1/chatmessages/{id}
  async deleteChatMessage(id: string): Promise<void> {
    await this.axiosInstance.delete(`/chatmessages/${id}`);
  }

  // GET /api/v1/users/{id}
  async getUser(id: string): Promise<GetUserByIdResponse> {
    const response = await this.axiosInstance.get<GetUserByIdResponse>(`/users/${id}`);
    return response.data;
  }

  // DELETE /api/v1/users/{id}
  async deleteUser(id: string): Promise<void> {
    await this.axiosInstance.delete(`/users/${id}`);
  }

  // GET /api/v1/users
  async getAllUsers(params: {
    sort: string;
    offset: number;
    pageSize: number;
    filter?: string;
  }): Promise<GetAllUserResponse> {
    const response = await this.axiosInstance.get<GetAllUserResponse>('/users', { params });
    return response.data;
  }

  // GET /api/v1/documents/{id}
  async getDocument(id: string): Promise<GetDocumentByIdResponse> {
    const response = await this.axiosInstance.get<GetDocumentByIdResponse>(`/documents/${id}`);
    return response.data;
  }

  // DELETE /api/v1/documents/{id}
  async deleteDocument(id: string): Promise<void> {
    await this.axiosInstance.delete(`/documents/${id}`);
  }

  // GET /api/v1/documents
  async getAllDocuments(params: {
    sort: string;
    offset: number;
    pageSize: number;
    filter?: string;
  }): Promise<GetAllDocumentResponse> {
    const response = await this.axiosInstance.get<GetAllDocumentResponse>('/documents', { params });
    return response.data;
  }



}
