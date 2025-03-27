/*==============================================================*/
/* Required Extensions */
/*==============================================================*/
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

/*==============================================================*/
/* Table: Embedding Models                                 */
/*==============================================================*/
CREATE TABLE embedding_models (
                                  id SERIAL PRIMARY KEY,
                                  name VARCHAR(255) NOT NULL UNIQUE,
                                  version VARCHAR(50) NOT NULL,
                                  dimensions INTEGER NOT NULL CHECK (dimensions > 0),
                                  created_at TIMESTAMPTZ NOT NULL ,
                                  updated_at TIMESTAMPTZ NOT NULL 
);

/*==============================================================*/
/* Tabela: Projects                                             */
/*==============================================================*/
CREATE TABLE projects (
                          id UUID PRIMARY KEY ,
                          name VARCHAR(255) NOT NULL,
                          system_prompt TEXT NOT NULL,
                          metadata JSONB NOT NULL ,
                          created_at TIMESTAMPTZ NOT NULL ,
                          updated_at TIMESTAMPTZ NOT NULL 
);

/*==============================================================*/
/* Table: Users                                              */
/* password_hash = hash(password + salt + pepper) */
/* password_hash = argon2(password + RandomNumberGenerator.Create() + secret_key_pepper) */
CREATE TABLE users (
                       id UUID PRIMARY KEY,
                       email VARCHAR(255) NOT NULL UNIQUE,
                       name VARCHAR(255) NOT NULL,                     
                       password_hash VARCHAR(255) NOT NULL,
                       salt VARCHAR(128) NOT NULL,  
                       metadata JSONB NOT NULL,
                       created_at TIMESTAMPTZ NOT NULL,
                       updated_at TIMESTAMPTZ NOT NULL
);



/*==============================================================*/
/* Tabela: Documents Status                                     */
/*==============================================================*/

CREATE TABLE  document_status (
                                  id int PRIMARY KEY,
                                  name varchar(50) NOT NULL UNIQUE,
                                  description text
);



/*==============================================================*/
/* Tabela: Documents                                           */
/*==============================================================*/
CREATE TABLE documents (
                           id UUID PRIMARY KEY ,
                           project_id UUID REFERENCES projects(id) ON DELETE CASCADE,
                           source_uri VARCHAR(1024) NOT NULL,
                           raw_content TEXT NOT NULL,
                           processed_content TEXT NOT NULL,
                           checksum VARCHAR(64) NOT NULL,
                           status_id int NOT NULL DEFAULT 1 REFERENCES document_status(id),
                           error_code varchar(50),
                           error_message text,
                           retry_count int NOT NULL DEFAULT 0,
                           last_processed_at timestamptz,
                           metadata JSONB NOT NULL ,
                           chunking_strategy VARCHAR(50) NOT NULL,
                           created_at TIMESTAMPTZ NOT NULL ,
                           updated_at TIMESTAMPTZ NOT NULL 

);

/*==============================================================*/


/*==============================================================*/
/* Table: Document Chunks                                 */
/*==============================================================*/
CREATE TABLE document_chunks (
                                 id UUID PRIMARY KEY ,
                                 document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
                                 chunk_number INTEGER NOT NULL,
                                 content TEXT NOT NULL,
                                 status_id int REFERENCES document_status(id),
                                 processing_error text,
                                 embedding_error text,
                                 token_count INTEGER NOT NULL CHECK (token_count > 0),
                                 start_offset INTEGER NOT NULL,
                                 end_offset INTEGER NOT NULL,
                                 language_code VARCHAR(3),
                                 metadata JSONB NOT NULL ,
                                 search_vector TSVECTOR,
                                 created_at TIMESTAMPTZ NOT NULL ,
                                 CONSTRAINT valid_offsets CHECK (start_offset < end_offset)
);

/*==============================================================*/
/* Table: Chunk Embeddings                                */
/*==============================================================*/
CREATE TABLE chunk_embeddings (
                                  chunk_id UUID PRIMARY KEY REFERENCES document_chunks(id) ON DELETE CASCADE,
                                  model_id INTEGER NOT NULL REFERENCES embedding_models(id),
                                  embedding VECTOR(384) NOT NULL,
                                  created_at TIMESTAMPTZ NOT NULL 
);

/*==============================================================*/
/* Table: Chat Sessions                                */
/*==============================================================*/
CREATE TABLE chat_sessions (
                               id UUID PRIMARY KEY ,
                               project_id UUID NULL REFERENCES projects(id) ON DELETE CASCADE,
                               title VARCHAR(255) NOT NULL,
                               metadata JSONB NOT NULL ,
                               created_at TIMESTAMPTZ NOT NULL ,
                               ended_at TIMESTAMPTZ
);

/*==============================================================*/
/* Tabela: Mensagens de Chat                                    */
/*==============================================================*/
CREATE TABLE chat_messages (
                               id UUID PRIMARY KEY ,
                               session_id UUID NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
                               role VARCHAR(20) NOT NULL CHECK (role IN ('user', 'assistant', 'system')),
                               content TEXT NOT NULL,
                               query_embedding VECTOR(384),
                               reference_chunk_ids UUID[],
                               metadata JSONB NOT NULL ,
                               created_at TIMESTAMPTZ NOT NULL 
);

/*==============================================================*/
/* Indexes and Optimizations                                     */
/*==============================================================*/
-- Indices for vectors
CREATE INDEX ON chunk_embeddings USING hnsw (embedding vector_cosine_ops);
CREATE INDEX ON chat_messages USING hnsw (query_embedding vector_cosine_ops);

-- Indexes for full-text search
CREATE INDEX ON document_chunks USING GIN(search_vector);

-- Índices for document statuses
CREATE INDEX idx_documents_status ON documents(status_id);
CREATE INDEX idx_documents_errors ON documents(error_code) WHERE error_code IS NOT NULL;

-- Performance indices
CREATE INDEX ON documents(project_id);
CREATE INDEX ON document_chunks(document_id);
CREATE INDEX idx_chunks_status ON document_chunks(status_id);
CREATE INDEX ON chat_sessions(project_id);
CREATE INDEX ON chat_messages(session_id);

/*==============================================================*/
/* Functions and Triggers                                      */
/*==============================================================*/
-- Automatic update of search_vector
CREATE OR REPLACE FUNCTION update_search_vector() RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector = to_tsvector(
        CASE NEW.language_code
            WHEN 'spa' THEN 'spanish'::regconfig  -- Explicit cast
            WHEN 'fra' THEN 'french'::regconfig
            WHEN 'por' THEN 'portuguese'::regconfig
            ELSE 'english'::regconfig
        END, 
        COALESCE(NEW.content, '')
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER search_vector_update
    BEFORE INSERT OR UPDATE ON document_chunks
                         FOR EACH ROW EXECUTE FUNCTION update_search_vector();


/*==============================================================*/
/* Hybrid Search Function                                 */
/*==============================================================*/

CREATE OR REPLACE FUNCTION hybrid_rag_search(
    query_embedding VECTOR(384),
    query_text TEXT,
    match_count INT = 5,
    similarity_threshold FLOAT = 0.5,
    user_id UUID = NULL,
    project_id UUID = NULL
) RETURNS TABLE (
                    document_id UUID,
                    chunk_id UUID,
                    content TEXT,
                    similarity DOUBLE PRECISION,
                    ts_rank REAL,
                    metadata JSONB
                ) AS $$
BEGIN
    RETURN QUERY
        SELECT
            d.id as document_id,
            dc.id as chunk_id,
            dc.content as content,
            1 - (ce.embedding <=> query_embedding)::DOUBLE PRECISION AS similarity,
            ts_rank(dc.search_vector, plainto_tsquery(query_text)):: REAL AS ts_rank,
            dc.metadata
        FROM chunk_embeddings ce
                 JOIN document_chunks dc ON ce.chunk_id = dc.id
                 JOIN documents d ON dc.document_id = d.id
        WHERE 1 - (ce.embedding <=> query_embedding) > similarity_threshold
          AND (hybrid_rag_search.project_id IS NULL OR d.project_id = hybrid_rag_search.project_id)
          AND (hybrid_rag_search.user_id IS NULL OR d.metadata->>'userId' = hybrid_rag_search.user_id::text)
        ORDER BY (similarity + ts_rank) DESC
        LIMIT match_count;
END;
$$ LANGUAGE plpgsql;



CREATE INDEX idx_projects_created_at ON projects(created_at);
CREATE INDEX idx_documents_metadata ON documents USING gin (metadata);
CREATE INDEX idx_document_chunks_chunk_number ON document_chunks(chunk_number);
CREATE INDEX idx_document_chunks_offsets ON document_chunks(start_offset, end_offset);


CREATE TABLE http_logs (
                           id UUID PRIMARY KEY ,
                           url TEXT NOT NULL,
                           method VARCHAR(10) NOT NULL,
                           request_headers JSONB NOT NULL,
                           request_body TEXT,
                           response_headers JSONB,
                           response_body TEXT,
                           status_code SMALLINT,
                           error_message TEXT,
                           duration_ms INTEGER,
                           service VARCHAR(50) NOT NULL,
                           metadata JSONB NOT NULL ,
                           created_at TIMESTAMPTZ NOT NULL ,
                           updated_at TIMESTAMPTZ NOT NULL ,

                           CONSTRAINT idx_http_created_check CHECK (created_at <= updated_at)
);

CREATE INDEX idx_http_created ON http_logs(created_at);
CREATE INDEX idx_http_status_code ON http_logs(status_code);
CREATE INDEX idx_http_service ON http_logs(service);
CREATE INDEX idx_http_metadata ON http_logs USING gin (metadata);


CREATE TABLE audit_logs (
                            id UUID PRIMARY KEY ,
                            event_type VARCHAR(200) NOT NULL,
                            event_data JSONB NOT NULL,
                            timestamp TIMESTAMPTZ NOT NULL ,
                            user_id VARCHAR,
                            metadata JSONB NOT NULL 
);


CREATE TABLE rag_experiments (
                                 id SERIAL PRIMARY KEY,
                                 name VARCHAR(250) NOT NULL,
                                 created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
                                 rag_options_json TEXT,
                                 average_precision DOUBLE PRECISION,
                                 average_recall DOUBLE PRECISION,
                                 average_f1 DOUBLE PRECISION
);

INSERT INTO document_status (id, name, description) VALUES
                                                        (1, 'Pending', 'Document received, waiting for processing'),
                                                        (2, 'Processing', 'Processing chunking/embedding'),
                                                        (3, 'Completed', 'Processing completed successfully'),
                                                        (4, 'Failed', 'Processing failed');
INSERT INTO embedding_models values (1,
                                     'all-minilm:l6-v2',
                                     'l6-v2',
                                     384,
                                     now(),
                                     now()) 