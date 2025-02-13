-- Cria a extensão para vetores
CREATE
EXTENSION IF NOT EXISTS vector;

-- Cria a tabela de documentos
CREATE TABLE IF NOT EXISTS documents
(
    id
    SERIAL
    PRIMARY
    KEY,
    content
    TEXT
    NOT
    NULL,
    metadata
    JSONB,
    embedding
    VECTOR
(
    384
), -- O tamanho do vetor é 384, de acordo com o seu modelo
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
    );

-- Cria o índice para a busca de vetores
CREATE INDEX IF NOT EXISTS documents_embedding_idx ON documents USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- Cria a função para buscar documentos semelha ntes
CREATE
OR REPLACE FUNCTION match_documents(
    query_embedding VECTOR(384),
    match_count INT DEFAULT 5,
    similarity_threshold FLOAT DEFAULT 0.5
)
RETURNS TABLE (
    id BIGINT,
    content TEXT,
    metadata JSONB,
    similarity FLOAT
)
LANGUAGE plpgsql AS $$
BEGIN
RETURN QUERY
SELECT id,
       content,
       metadata,
       1 - (embedding <=> query_embedding) AS similarity
FROM documents
WHERE 1 - (embedding <=> query_embedding) > similarity_threshold
ORDER BY embedding <=> query_embedding
    LIMIT match_count;
END;
$$;
