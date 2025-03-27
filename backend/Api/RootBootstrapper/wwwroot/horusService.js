class HorusService {
    constructor(baseURL) {
        this.api = axios.create({
            baseURL: baseURL,
            headers: {
                'Content-Type': 'application/json',
            },
        });
    }

    // Método auxiliar para validação de parâmetros obrigatórios
    _validateRequiredParams(params, requiredFields) {
        requiredFields.forEach(field => {
            if (params[field] === undefined || params[field] === null) {
                throw new Error(`Parâmetro obrigatório ausente: ${field}`);
            }
        });
    }


    async sendMessage(text, userId, chatSessionId) {
        try {
            if(chatSessionId == null) {
                const newGuid = generateGUID()
                chatSessionId = newGuid;
                await this.createChatSession(text, userId, newGuid);
            }

            const response = await this.generateText(text, userId, chatSessionId)
            return {text: response.text, chatSessionId};
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para gerar texto
    async generateText(prompt, userId, chatSessionId) {
        try {
            const response = await this.api.post('/generate-text', new GenerateTextQuery(prompt, userId, chatSessionId));
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para adicionar documento
    async addDocument(request) {
        this._validateRequiredParams(request, ['projectId', 'name']);
        const formData = new FormData();
        formData.append('request', JSON.stringify(request));
        if (request.fileContent) {
            formData.append('fileContent', request.fileContent);
        }
        try {
            const response = await this.api.post('/add-document', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para criar projeto
    async createProject(command) {
        this._validateRequiredParams(command, ['name']);
        try {
            const response = await this.api.post('/api/v1/projects', command);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter todos os projetos
    async getAllProjects(params) {
        this._validateRequiredParams(params, ['sort', 'offset', 'pageSize']);
        try {
            const response = await this.api.get('/api/v1/projects', {params});
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter projeto por ID
    async getProject(id) {
        if (!id) throw new Error('ID do projeto é obrigatório');
        try {
            const response = await this.api.get(`/api/v1/projects/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para atualizar projeto
    async updateProject(id, command) {
        if (!id) throw new Error('ID do projeto é obrigatório');
        try {
            const response = await this.api.put(`/api/v1/projects/${id}`, command);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para deletar projeto
    async deleteProject(id) {
        if (!id) throw new Error('ID do projeto é obrigatório');
        try {
            const response = await this.api.delete(`/api/v1/projects/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    
    // Método para criar sessão de chat
    async createChatSession(initialPrompt, userId, newGuid) {
        try {
            await this.api.post('/api/v1/chatsessions', new CreateChatSessionCommand(
                newGuid,
                null,
                "new chat",
                null,
                {}
            ));
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter todas as sessões de chat
    async getAllChatSessions(params) {
        this._validateRequiredParams(params, ['sort', 'offset', 'pageSize']);
        try {
            const response = await this.api.get('/api/v1/chatsessions', {params});
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter sessão de chat por ID
    async getChatSession(id) {
        if (!id) throw new Error('ID da sessão de chat é obrigatório');
        try {
            const response = await this.api.get(`/api/v1/chatsessions/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    async getChatHistory(id) {
        if (!id) throw new Error('ID da sessão de chat é obrigatório');
        try {
            let response = await this.getChatSession(id)
            return response;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para atualizar sessão de chat
    async updateChatSession(id, command) {
        if (!id) throw new Error('ID da sessão de chat é obrigatório');
        try {
            const response = await this.api.put(`/api/v1/chatsessions/${id}`, command);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para deletar sessão de chat
    async deleteChatSession(id) {
        if (!id) throw new Error('ID da sessão de chat é obrigatório');
        try {
            const response = await this.api.delete(`/api/v1/chatsessions/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para criar mensagem de chat
    async createChatMessage(command) {
        this._validateRequiredParams(command, ['sessionId', 'role', 'content']);
        try {
            const response = await this.api.post('/api/v1/chatmessages', command);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter todas as mensagens de chat
    async getAllChatMessages(params) {
        this._validateRequiredParams(params, ['sort', 'offset', 'pageSize']);
        try {
            const response = await this.api.get('/api/v1/chatmessages', {params});
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para obter mensagem de chat por ID
    async getChatMessage(id) {
        if (!id) throw new Error('ID da mensagem de chat é obrigatório');
        try {
            const response = await this.api.get(`/api/v1/chatmessages/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para atualizar mensagem de chat
    async updateChatMessage(id, command) {
        if (!id) throw new Error('ID da mensagem de chat é obrigatório');
        try {
            const response = await this.api.put(`/api/v1/chatmessages/${id}`, command);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }

    // Método para deletar mensagem de chat
    async deleteChatMessage(id) {
        if (!id) throw new Error('ID da mensagem de chat é obrigatório');
        try {
            const response = await this.api.delete(`/api/v1/chatmessages/${id}`);
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : error;
        }
    }
}


class GenerateTextQuery {
    constructor(prompt, userInfo = null, chatSessionId = null) {
        if (!prompt) throw new Error('O campo "prompt" é obrigatório');
        this.prompt = prompt;
        this.userInfo = userInfo ? { 
            id: userInfo,
            chatSessionId 
        } : null;
    }
}

class AddDocumentCommandRequest {
    constructor(projectId, name, sourceUri = null, fileContent = null, chunkingStrategy = null) {
        if (!projectId) throw new Error('O campo "projectId" é obrigatório');
        if (!name) throw new Error('O campo "name" é obrigatório');
        this.projectId = projectId;
        this.name = name;
        this.sourceUri = sourceUri;
        this.fileContent = fileContent;
        this.chunkingStrategy = chunkingStrategy;
    }
}

class CreateProjectCommand {
    constructor(id = null, name, systemPrompt = null, metadata = {}) {
        if (!name) throw new Error('O campo "name" é obrigatório');
        this.id = id;
        this.name = name;
        this.systemPrompt = systemPrompt;
        this.metadata = metadata;
    }
}

class GetAllProjectsQuery {
    constructor(sort, offset, pageSize, filter = null) {
        if (!sort) throw new Error('O campo "sort" é obrigatório');
        if (offset === undefined || offset === null) throw new Error('O campo "offset" é obrigatório');
        if (pageSize === undefined || pageSize === null) throw new Error('O campo "pageSize" é obrigatório');
        this.sort = sort;
        this.offset = offset;
        this.pageSize = pageSize;
        this.filter = filter;
    }
}


class UpdateProjectCommand {
    constructor(name = null, systemPrompt = null, metadata = {}) {
        this.name = name;
        this.systemPrompt = systemPrompt;
        this.metadata = metadata;
    }
}

class CreateChatSessionCommand {
    constructor(id = null, projectId, title = null, endedAt = null, metadata = {}) {
        this.id = id;
        this.projectId = projectId;
        this.title = title;
        this.endedAt = endedAt;
        this.metadata = metadata;
    }
}

class GetAllChatSessionsQuery {
    constructor(sort, offset, pageSize, filter = null) {
        if (!sort) throw new Error('O campo "sort" é obrigatório');
        if (offset === undefined || offset === null) throw new Error('O campo "offset" é obrigatório');
        if (pageSize === undefined || pageSize === null) throw new Error('O campo "pageSize" é obrigatório');
        this.sort = sort;
        this.offset = offset;
        this.pageSize = pageSize;
        this.filter = filter;
    }
}


class UpdateChatSessionCommand {
    constructor(projectId = null, title = null, endedAt = null, metadata = {}) {
        this.projectId = projectId;
        this.title = title;
        this.endedAt = endedAt;
        this.metadata = metadata;
    }
}

class CreateChatMessageCommand {
    constructor(id = null, sessionId, role, content, queryEmbedding = null, referenceChunkIds = [], metadata = {}) {
        if (!sessionId) throw new Error('O campo "sessionId" é obrigatório');
        if (!role) throw new Error('O campo "role" é obrigatório');
        if (!content) throw new Error('O campo "content" é obrigatório');
        this.id = id;
        this.sessionId = sessionId;
        this.role = role;
        this.content = content;
        this.queryEmbedding = queryEmbedding;
        this.referenceChunkIds = referenceChunkIds;
        this.metadata = metadata;
    }
}

class GetAllChatMessagesQuery {
    constructor(sort, offset, pageSize, filter = null) {
        if (!sort) throw new Error('O campo "sort" é obrigatório');
        if (offset === undefined || offset === null) throw new Error('O campo "offset" é obrigatório');
        if (pageSize === undefined || pageSize === null) throw new Error('O campo "pageSize" é obrigatório');
        this.sort = sort;
        this.offset = offset;
        this.pageSize = pageSize;
        this.filter = filter;
    }
}

class UpdateChatMessageCommand {
    constructor(sessionId = null, role = null, content = null, queryEmbedding = null, referenceChunkIds = [], metadata = {}) {
        this.sessionId = sessionId;
        this.role = role;
        this.content = content;
        this.queryEmbedding = queryEmbedding;
        this.referenceChunkIds = referenceChunkIds;
        this.metadata = metadata;
    }
}

function generateGUID() {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
        s4() + '-' + s4() + s4() + s4();
}